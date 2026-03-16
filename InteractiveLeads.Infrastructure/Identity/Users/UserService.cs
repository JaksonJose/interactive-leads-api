using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Constants;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Users;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Identity.Users
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _tenantContextAccessor;
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly SysAdminSeedSettings _sysAdminSeed;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context,
            IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor,
            ISubscriptionPlanService subscriptionPlanService,
            IOptions<SysAdminSeedSettings> sysAdminSeed)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _tenantContextAccessor = tenantContextAccessor;
            _subscriptionPlanService = subscriptionPlanService;
            _sysAdminSeed = sysAdminSeed.Value;
        }

        public async Task<ResultResponse> ActivateOrDeactivateAsync(Guid userId, bool activation)
        {
            var userInDb = await GetUserAsync(userId);

            userInDb.IsActive = activation;

            var result = await _userManager.UpdateAsync(userInDb);

            if (!result.Succeeded)
            {
                var identityResponse = new ResultResponse();
                foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(result))
                {
                    identityResponse.AddErrorMessage(error, "identity.operation_failed");
                }

                throw new IdentityException(identityResponse);
            }

            var response = new ResultResponse();
            response.AddSuccessMessage("User status updated successfully", "user.status_updated_successfully");
            return response;
        }

        public async Task<ResultResponse> AssignRolesAsync(Guid userId, UserRolesRequest request)
        {
            var userInDb = await GetUserAsync(userId);

            if (await _userManager.IsInRoleAsync(userInDb, RoleConstants.SysAdmin)
                && request.UserRoles.Any(ur => !ur.IsAssigned && ur.Name == RoleConstants.SysAdmin))
            {
                var adminUsersCount = (await _userManager.GetUsersInRoleAsync(RoleConstants.SysAdmin)).Count;
                bool isGlobalSysAdmin = userInDb.TenantId == null && string.Equals(userInDb.Email, _sysAdminSeed.Email, StringComparison.OrdinalIgnoreCase);
                if (isGlobalSysAdmin)
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage("Not allowed to remove Admin role for the global SysAdmin user.", "user.admin_role_removal_not_allowed");
                    throw new ConflictException(conflictResponse);
                }
                if (adminUsersCount <= 2)
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage("Not allowed. Tenant should have at least two Admin Users.", "user.min_admin_users_required");
                    throw new ConflictException(conflictResponse);
                }
            }

            foreach (var userRole in request.UserRoles)
            {
                if (userRole.IsAssigned)
                {
                    if (!await _userManager.IsInRoleAsync(userInDb, userRole.Name))
                    {
                        await _userManager.AddToRoleAsync(userInDb, userRole.Name);
                    }
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(userInDb, userRole.Name);
                }
            }

            var response = new ResultResponse();
            response.AddSuccessMessage("User roles updated successfully", "user.roles_updated_successfully");
            return response;
        }

        public async Task<ResultResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {
            var userInDb = await GetUserAsync(request.UserId);

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage("Passwords do not match.", "user.passwords_not_match");
                throw new ConflictException(conflictResponse);
            }

            var result = await _userManager.ChangePasswordAsync(userInDb, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                var identityResponse = new ResultResponse();
                foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(result))
                {
                    identityResponse.AddErrorMessage(error, "identity.operation_failed");
                }
                throw new IdentityException(identityResponse);
            }

            var response = new ResultResponse();
            response.AddSuccessMessage("Password changed successfully", "user.password_changed_successfully");
            return response;
        }

        public async Task<ResultResponse> CreateAsync(CreateUserRequest request)
        {
            if (request.Password != request.ConfirmPassword)
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage("Passwords do not match.", "user.passwords_not_match");
                throw new ConflictException(conflictResponse);
            }

            if (await IsEmailTakenAsync(request.Email))
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage("Email already taken.", "user.email_already_taken");
                throw new ConflictException(conflictResponse);
            }

            var tenantId = _tenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
            if (string.IsNullOrEmpty(tenantId))
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Tenant context is not available.", "tenant.context_not_available");
                throw new ConflictException(errorResponse);
            }

            var currentUserCount = await _context.Users.CountAsync(u => u.TenantId == tenantId);
            var withinLimit = await _subscriptionPlanService.CheckLimitAsync(tenantId, BillingSeed.LimitKeys.Users, currentUserCount, 1);
            if (!withinLimit)
            {
                var limitResponse = new ResultResponse();
                limitResponse.AddErrorMessage("User limit reached for your plan. Please upgrade to add more users.", ErrorKeys.SUBSCRIPTION_PLAN_LIMIT_REACHED);
                throw new ConflictException(limitResponse);
            }

            var newUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IsActive = request.IsActive,
                UserName = request.Email,
                EmailConfirmed = true,
                TenantId = tenantId
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                var identityResponse = new ResultResponse();
                foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(result))
                {
                    identityResponse.AddErrorMessage(error, "identity.operation_failed");
                }
                throw new IdentityException(identityResponse);
            }

            // Assign roles if provided
            if (request.Roles != null && request.Roles.Any())
            {
                // Filter out restricted roles (SysAdmin, Support, Owner cannot be assigned through this feature)
                var restrictedRoles = new[] { RoleConstants.SysAdmin, RoleConstants.Support, RoleConstants.Owner };
                var restrictedRolesInRequest = request.Roles.Intersect(restrictedRoles).ToList();
                
                if (restrictedRolesInRequest.Any())
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage($"Cannot assign restricted roles: {string.Join(", ", restrictedRolesInRequest)}", "user.restricted_roles_not_allowed");
                    throw new ConflictException(conflictResponse);
                }

                var allowedRoles = request.Roles.Except(restrictedRoles).ToList();
                if (allowedRoles.Any())
                {
                    var roleResult = await _userManager.AddToRolesAsync(newUser, allowedRoles);
                    if (!roleResult.Succeeded)
                    {
                        var identityResponse = new ResultResponse();
                        foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(roleResult))
                        {
                            identityResponse.AddErrorMessage($"Failed to assign role: {error}", "user.role_assignment_failed");
                        }
                        throw new IdentityException(identityResponse);
                    }
                }
            }

            var response = new ResultResponse();
            response.AddSuccessMessage("User created successfully", "user.created_successfully");
            return response;
        }

        public async Task<Guid> CreateUserForInvitationAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
        {
            if (await IsEmailTakenAsync(request.Email))
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage("Email already taken.", "user.email_already_taken");
                throw new ConflictException(conflictResponse);
            }

            var tenantId = _tenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
            if (string.IsNullOrEmpty(tenantId))
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Tenant context is not available.", "tenant.context_not_available");
                throw new ConflictException(errorResponse);
            }

            var currentUserCount = await _context.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken);
            var withinLimit = await _subscriptionPlanService.CheckLimitAsync(tenantId, BillingSeed.LimitKeys.Users, currentUserCount, 1);
            if (!withinLimit)
            {
                var limitResponse = new ResultResponse();
                limitResponse.AddErrorMessage("User limit reached for your plan. Please upgrade to add more users.", ErrorKeys.SUBSCRIPTION_PLAN_LIMIT_REACHED);
                throw new ConflictException(limitResponse);
            }

            var temporaryPassword = Guid.NewGuid().ToString("N") + "Aa1!"; // User will set real password on activation
            var newUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IsActive = false,
                UserName = request.Email,
                EmailConfirmed = true,
                TenantId = tenantId
            };

            var result = await _userManager.CreateAsync(newUser, temporaryPassword);
            if (!result.Succeeded)
            {
                var identityResponse = new ResultResponse();
                foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(result))
                {
                    identityResponse.AddErrorMessage(error, "identity.operation_failed");
                }
                throw new IdentityException(identityResponse);
            }

            if (request.Roles != null && request.Roles.Any())
            {
                var restrictedRoles = new[] { RoleConstants.SysAdmin, RoleConstants.Support, RoleConstants.Owner };
                var restrictedRolesInRequest = request.Roles.Intersect(restrictedRoles).ToList();
                if (restrictedRolesInRequest.Any())
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage($"Cannot assign restricted roles: {string.Join(", ", restrictedRolesInRequest)}", "user.restricted_roles_not_allowed");
                    throw new ConflictException(conflictResponse);
                }
                var allowedRoles = request.Roles.Except(restrictedRoles).ToList();
                if (allowedRoles.Any())
                {
                    var roleResult = await _userManager.AddToRolesAsync(newUser, allowedRoles);
                    if (!roleResult.Succeeded)
                    {
                        var identityResponse = new ResultResponse();
                        foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(roleResult))
                        {
                            identityResponse.AddErrorMessage($"Failed to assign role: {error}", "user.role_assignment_failed");
                        }
                        throw new IdentityException(identityResponse);
                    }
                }
            }

            return newUser.Id;
        }

        public async Task SetPasswordAndActivateAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(userId);
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                await _userManager.RemovePasswordAsync(user);
            }
            var addResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addResult.Succeeded)
            {
                var identityResponse = new ResultResponse();
                foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(addResult))
                {
                    identityResponse.AddErrorMessage(error, "identity.operation_failed");
                }
                throw new IdentityException(identityResponse);
            }
            user.IsActive = true;
            await _userManager.UpdateAsync(user);
        }

        public async Task<ResultResponse> CreateSupportUserAsync(CreateUserRequest request)
        {
            if (request.Password != request.ConfirmPassword)
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage("Passwords do not match.", "user.passwords_not_match");
                throw new ConflictException(conflictResponse);
            }

            if (await IsEmailTakenAsync(request.Email))
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage("Email already taken.", "user.email_already_taken");
                throw new ConflictException(conflictResponse);
            }

            // Only Support role is allowed for global support users
            var allowedRoles = new[] { RoleConstants.Support };
            if (request.Roles != null && request.Roles.Any())
            {
                var invalidRoles = request.Roles.Except(allowedRoles).ToList();
                if (invalidRoles.Any())
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage($"Only the Support role can be assigned. Invalid: {string.Join(", ", invalidRoles)}", "user.support_user_invalid_roles");
                    throw new ConflictException(conflictResponse);
                }
            }

            var newUser = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IsActive = request.IsActive,
                UserName = request.Email,
                EmailConfirmed = true,
                TenantId = null
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
            {
                var identityResponse = new ResultResponse();
                foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(result))
                {
                    identityResponse.AddErrorMessage(error, "identity.operation_failed");
                }
                throw new IdentityException(identityResponse);
            }

            await _userManager.AddToRoleAsync(newUser, RoleConstants.Support);

            var response = new ResultResponse();
            response.AddSuccessMessage("Support user created successfully", "user.support_created_successfully");
            return response;
        }

        public async Task<ListResponse<UserResponse>> GetGlobalUsersAsync(CancellationToken ct)
        {
            var usersInDb = await _userManager.Users
                .Where(u => u.TenantId == null)
                .ToListAsync(ct);

            var userResponses = usersInDb.Adapt<List<UserResponse>>();

            foreach (var userResponse in userResponses)
            {
                var user = usersInDb.FirstOrDefault(u => u.Id.ToString() == userResponse.Id);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userResponse.Roles = roles.ToList();
                }
            }

            return new ListResponse<UserResponse>(userResponses, userResponses.Count);
        }

        public async Task<ResultResponse> DeleteAsync(Guid userId)
        {
            var userInDb = await GetUserAsync(userId);

            if (userInDb.TenantId == null && string.Equals(userInDb.Email, _sysAdminSeed.Email, StringComparison.OrdinalIgnoreCase))
            {
                var conflictResponse = new ResultResponse();
                conflictResponse.AddErrorMessage("Not allowed to remove the global SysAdmin user.", "user.global_admin_deletion_not_allowed");
                throw new ConflictException(conflictResponse);
            }

            _context.Users.Remove(userInDb);
            await _context.SaveChangesAsync();

            var response = new ResultResponse();
            response.AddSuccessMessage("User deleted successfully", "user.deleted_successfully");
            return response;
        }

        public async Task<ListResponse<UserResponse>> GetAllAsync(CancellationToken ct)
        {
            var usersInDb = await _userManager.Users.ToListAsync(ct);

            var userResponses = usersInDb.Adapt<List<UserResponse>>();
            
            // Populate roles for each user
            foreach (var userResponse in userResponses)
            {
                var user = usersInDb.FirstOrDefault(u => u.Id.ToString() == userResponse.Id);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userResponse.Roles = roles.ToList();
                }
            }
            
            var response = new ListResponse<UserResponse>(userResponses, userResponses.Count);

            return response;
        }

        public async Task<SingleResponse<UserResponse>> GetByIdAsync(Guid userId, CancellationToken ct)
        {
            var userInDb = await GetUserAsync(userId);

            var userResponse = userInDb.Adapt<UserResponse>();
            
            // Populate roles for the user
            var roles = await _userManager.GetRolesAsync(userInDb);
            userResponse.Roles = roles.ToList();
            
            var response = new SingleResponse<UserResponse>(userResponse);
            return response;
        }

        public async Task<SingleResponse<UserResponse>> GetByEmailAsync(string email, CancellationToken ct)
        {
            var userInDb = await _userManager.FindByEmailAsync(email);
            if (userInDb == null)
            {
                var notFoundResponse = new SingleResponse<UserResponse>();
                notFoundResponse.AddErrorMessage("User not found with the provided email.", "user.not_found");
                return notFoundResponse;
            }

            var userResponse = userInDb.Adapt<UserResponse>();
            
            // Populate roles for the user
            var roles = await _userManager.GetRolesAsync(userInDb);
            userResponse.Roles = roles.ToList();
            
            var response = new SingleResponse<UserResponse>(userResponse);
            return response;
        }

        public async Task<ListResponse<UserRoleResponse>> GetUserRolesAsync(Guid userId, CancellationToken ct)
        {
            var userInDb = await GetUserAsync(userId);

            var userRoles = new List<UserRoleResponse>();

            var rolesInDb = await _roleManager.Roles.ToListAsync(ct);

            foreach (var role in rolesInDb)
            {
                userRoles.Add(new UserRoleResponse
                {
                    RoleId = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    IsAssigned = await _userManager.IsInRoleAsync(userInDb, role.Name!),
                });
            }

            var response = new ListResponse<UserRoleResponse>(userRoles, userRoles.Count);
            return response;
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) is not null;
        }

        public async Task<ResultResponse> UpdateAsync(UpdateUserRequest request)
        {
            var userInDb = await GetUserAsync(request.Id);

            userInDb.FirstName = request.FirstName;
            userInDb.LastName = request.LastName;
            userInDb.PhoneNumber = request.PhoneNumber;

            var result = await _userManager.UpdateAsync(userInDb);

            if (!result.Succeeded)
            {
                var identityResponse = new ResultResponse();
                foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(result))
                {
                    identityResponse.AddErrorMessage(error, "identity.operation_failed");
                }
                throw new IdentityException(identityResponse);
            }

            // Update roles if provided
            if (request.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(userInDb);
                
                // Roles that cannot be assigned (but can be preserved if user already has them)
                var nonAssignableRoles = new[] { RoleConstants.SysAdmin, RoleConstants.Support };
                // Owner cannot be assigned, but can be preserved if user already has it
                var ownerRole = RoleConstants.Owner;
                
                // Check if trying to assign non-assignable roles
                var nonAssignableInRequest = request.Roles.Intersect(nonAssignableRoles).ToList();
                if (nonAssignableInRequest.Any())
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage($"Cannot assign restricted roles: {string.Join(", ", nonAssignableInRequest)}", "user.restricted_roles_not_allowed");
                    throw new ConflictException(conflictResponse);
                }
                
                // Check if trying to assign Owner to someone who doesn't have it
                var requestingOwner = request.Roles.Contains(ownerRole);
                var hasOwner = currentRoles.Contains(ownerRole);
                if (requestingOwner && !hasOwner)
                {
                    var conflictResponse = new ResultResponse();
                    conflictResponse.AddErrorMessage($"Cannot assign Owner role through this feature", "user.owner_role_not_allowed");
                    throw new ConflictException(conflictResponse);
                }
                
                // Preserve Owner if user already has it (even if not in request)
                var rolesToProcess = request.Roles.ToList();
                if (hasOwner && !rolesToProcess.Contains(ownerRole))
                {
                    rolesToProcess.Add(ownerRole);
                }
                
                // Preserve other restricted roles that user already has
                var currentRestrictedRoles = currentRoles.Intersect(nonAssignableRoles).ToList();
                var editableRoles = rolesToProcess.Union(currentRestrictedRoles).ToList();
                
                var rolesToAdd = editableRoles.Except(currentRoles).ToList();
                var rolesToRemove = currentRoles.Except(editableRoles).ToList();
                
                // Prevent removal of restricted roles
                var allRestrictedRoles = nonAssignableRoles.Union(new[] { ownerRole }).ToList();
                var restrictedRolesToRemove = rolesToRemove.Intersect(allRestrictedRoles).ToList();
                if (restrictedRolesToRemove.Any())
                {
                    rolesToRemove = rolesToRemove.Except(allRestrictedRoles).ToList();
                }

                if (rolesToRemove.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(userInDb, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        var identityResponse = new ResultResponse();
                        foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(removeResult))
                        {
                            identityResponse.AddErrorMessage($"Failed to remove role: {error}", "user.role_update_failed");
                        }
                        throw new IdentityException(identityResponse);
                    }
                }

                if (rolesToAdd.Any())
                {
                    var addResult = await _userManager.AddToRolesAsync(userInDb, rolesToAdd);
                    if (!addResult.Succeeded)
                    {
                        var identityResponse = new ResultResponse();
                        foreach (var error in IdentityHelper.GetIdentityResultErrorDescriptions(addResult))
                        {
                            identityResponse.AddErrorMessage($"Failed to add role: {error}", "user.role_update_failed");
                        }
                        throw new IdentityException(identityResponse);
                    }
                }
            }

            var response = new ResultResponse();
            response.AddSuccessMessage("User updated successfully", "user.updated_successfully");
            return response;
        }

        private async Task<ApplicationUser> GetUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                var notFoundResponse = new ResultResponse();
                notFoundResponse.AddErrorMessage("User does not exist.", "user.not_found");
                throw new NotFoundException(notFoundResponse);
            }
            return user;
        }
    }
}


using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Identity.Roles;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public class ApplicationDbSeeder(
        IMultiTenantContextAccessor<InteractiveTenantInfo> tenantInfoContextAccessor,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext,
        IServiceProvider serviceProvider,
        RoleSeeder roleSeeder)
    {
        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _tenantInfoContextAccessor = tenantInfoContextAccessor;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly RoleSeeder _roleSeeder = roleSeeder;

        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            if (_applicationDbContext.Database.GetMigrations().Any())
            {
                if ((await _applicationDbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any()) 
                { 
                    await _applicationDbContext.Database.MigrateAsync(cancellationToken);
                }

                if (await _applicationDbContext.Database.CanConnectAsync(cancellationToken))
                {
                    // Use the new RoleSeeder for comprehensive role initialization
                    await _roleSeeder.SeedRolesAsync(cancellationToken);
                    await InitializeAdminUserAsync();
                }
            }
        }

        // Legacy methods kept for backward compatibility but now handled by RoleSeeder
        private async Task InitializeAdminUserAsync()
        {
            if (string.IsNullOrEmpty(_tenantInfoContextAccessor.MultiTenantContext.TenantInfo?.Email)) return;

            if (await _userManager.Users.SingleOrDefaultAsync(user => user.Email == _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email) is not ApplicationUser incomingUser)
            {
                incomingUser = new ApplicationUser
                {
                    FirstName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.FirstName,
                    LastName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.LastName,
                    Email = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email,
                    UserName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email,
                    NormalizedEmail = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Email.ToUpperInvariant(),
                    NormalizedUserName = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo?.Email?.ToUpperInvariant(),
                    TenantId = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo.Id, // ← Set user's tenant
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true
                };

                var passwordHash = new PasswordHasher<ApplicationUser>();

                incomingUser.PasswordHash = passwordHash.HashPassword(incomingUser, TenancyConstants.DefaultPassword);
                await _userManager.CreateAsync(incomingUser);

                // Create user-tenant mapping for optimized performance
                await CreateUserTenantMappingAsync(incomingUser.Email, incomingUser.TenantId);
            }

            // Assign appropriate role based on tenant type
            string roleToAssign = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo?.Id == TenancyConstants.Root.Id 
                ? RoleConstants.SysAdmin 
                : RoleConstants.Owner;

            if (!await _userManager.IsInRoleAsync(incomingUser, roleToAssign))
            {
                await _userManager.AddToRoleAsync(incomingUser, roleToAssign);
            }
        }

        private async Task CreateUserTenantMappingAsync(string email, string tenantId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
                
                // Check if mapping already exists
                var existingMapping = await tenantDbContext.UserTenantMappings
                    .Where(m => m.Email == email)
                    .FirstOrDefaultAsync();
                
                if (existingMapping == null)
                {
                    var mapping = new UserTenantMapping
                    {
                        Email = email,
                        TenantId = tenantId,
                        IsActive = true
                    };
                    
                    tenantDbContext.UserTenantMappings.Add(mapping);
                    await tenantDbContext.SaveChangesAsync();
                }
            }
            catch
            {
                // Log error if needed, but don't fail the user creation
                // The system can still work with the fallback strategy
            }
        }
    }
}

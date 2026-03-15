using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractiveLeads.Infrastructure.Identity.Roles
{
    /// <summary>
    /// Service for seeding roles and permissions into the database.
    /// </summary>
    /// <remarks>
    /// Automatically creates all system roles with their appropriate permissions
    /// during database initialization.
    /// </remarks>
    public class RoleSeeder
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the RoleSeeder class.
        /// </summary>
        /// <param name="roleManager">The role manager for role operations.</param>
        /// <param name="context">The application database context.</param>
        public RoleSeeder(RoleManager<ApplicationRole> roleManager, ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Seeds all system roles. Authorization is role-based only; permissions are derived from PermissionConstants per role.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the seeding operation.</returns>
        public async Task<ResultResponse> SeedRolesAsync(CancellationToken cancellationToken = default)
        {
            var response = new ResultResponse();
            var createdRoles = new List<string>();

            try
            {
                await SeedRoleAsync(RoleConstants.SysAdmin, "System Administrator", "Full system access across all tenants", createdRoles, cancellationToken);
                await SeedRoleAsync(RoleConstants.Support, "Support User", "Limited cross-tenant access for customer support", createdRoles, cancellationToken);
                await SeedRoleAsync(RoleConstants.Owner, "Tenant Owner", "Full control over tenant operations", createdRoles, cancellationToken);
                await SeedRoleAsync(RoleConstants.Manager, "Tenant Manager", "User management within tenant", createdRoles, cancellationToken);
                await SeedRoleAsync(RoleConstants.Agent, "Tenant Agent", "Basic operations within tenant", createdRoles, cancellationToken);

                response.AddSuccessMessage($"Successfully seeded {createdRoles.Count} roles: {string.Join(", ", createdRoles)}", "roles.seeded_successfully");
            }
            catch (Exception ex)
            {
                response.AddErrorMessage($"Error seeding roles: {ex.Message}", "roles.seeding_failed");
            }

            return response;
        }

        /// <summary>
        /// Seeds a specific role (name and description only). No permission claims are stored.
        /// </summary>
        private async Task SeedRoleAsync(string roleName, string description, string longDescription,
            List<string> createdRoles, CancellationToken cancellationToken)
        {
            var existingRole = await _roleManager.FindByNameAsync(roleName);
            if (existingRole != null)
                return;

            var role = new ApplicationRole
            {
                Name = roleName,
                Description = description,
                NormalizedName = roleName.ToUpperInvariant()
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            createdRoles.Add(roleName);
        }

        /// <summary>
        /// Gets all seeded roles with permissions derived from PermissionConstants per role (no RoleClaims).
        /// </summary>
        public async Task<List<RoleWithPermissionsResponse>> GetSeededRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _context.Roles.ToListAsync(cancellationToken);

            return roles.Select(role => new RoleWithPermissionsResponse
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                Permissions = InteractivePermissions.GetPermissionsForRole(role.Name ?? string.Empty).Select(p => p.Name).ToList()
            }).ToList();
        }

        /// <summary>
        /// Clears all roles and permissions (for testing purposes).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the operation.</returns>
        public async Task<ResultResponse> ClearAllRolesAsync(CancellationToken cancellationToken = default)
        {
            var response = new ResultResponse();

            try
            {
                // Remove all role claims first
                var allRoleClaims = await _context.RoleClaims.ToListAsync(cancellationToken);
                _context.RoleClaims.RemoveRange(allRoleClaims);

                // Remove all roles
                var allRoles = await _context.Roles.ToListAsync(cancellationToken);
                _context.Roles.RemoveRange(allRoles);

                await _context.SaveChangesAsync(cancellationToken);

                response.AddSuccessMessage("All roles and permissions cleared successfully", "roles.cleared_successfully");
            }
            catch (Exception ex)
            {
                response.AddErrorMessage($"Error clearing roles: {ex.Message}", "roles.clear_failed");
            }

            return response;
        }
    }

    /// <summary>
    /// Response model for role with permissions.
    /// </summary>
    public class RoleWithPermissionsResponse
    {
        /// <summary>
        /// The role ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The role name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The role description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The role permissions.
        /// </summary>
        public List<string> Permissions { get; set; } = new();
    }
}

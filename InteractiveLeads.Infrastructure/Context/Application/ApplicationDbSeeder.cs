using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Identity.Roles;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public class ApplicationDbSeeder(
        IMultiTenantContextAccessor<InteractiveTenantInfo> tenantInfoContextAccessor,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext,
        IServiceProvider serviceProvider,
        RoleSeeder roleSeeder,
        IOptions<SysAdminSeedSettings> sysAdminSeed)
    {
        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _tenantInfoContextAccessor = tenantInfoContextAccessor;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly RoleSeeder _roleSeeder = roleSeeder;
        private readonly SysAdminSeedSettings _sysAdminSeed = sysAdminSeed.Value;

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
                    // Roles are global (same for all tenants): seed only once in global context.
                    var tenantInfo = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo;
                    if (tenantInfo?.Id == null)
                        await _roleSeeder.SeedRolesAsync(cancellationToken);

                    await InitializeAdminUserAsync();
                    await InitializeTenantOwnerAsync();
                }
            }
        }

        /// <summary>
        /// Creates global SysAdmin when in global context (TenantInfo.Id is null). No root tenant; no UserTenantMapping for global users.
        /// </summary>
        private async Task InitializeAdminUserAsync()
        {
            var tenantInfo = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo;
            bool isGlobalContext = tenantInfo?.Id == null;

            if (!isGlobalContext)
                return;

            if (string.IsNullOrWhiteSpace(_sysAdminSeed.Email))
                throw new InvalidOperationException("SysAdminSeed:Email is required in appsettings for global SysAdmin.");

            var existingUser = await _userManager.Users
                .Where(u => u.TenantId == null && u.Email == _sysAdminSeed.Email)
                .SingleOrDefaultAsync();
            if (existingUser != null)
            {
                if (!await _userManager.IsInRoleAsync(existingUser, RoleConstants.SysAdmin))
                    await _userManager.AddToRoleAsync(existingUser, RoleConstants.SysAdmin);
                return;
            }

            var incomingUser = new ApplicationUser
            {
                FirstName = _sysAdminSeed.FirstName ?? string.Empty,
                LastName = _sysAdminSeed.LastName ?? string.Empty,
                Email = _sysAdminSeed.Email.Trim(),
                UserName = _sysAdminSeed.Email.Trim(),
                NormalizedEmail = _sysAdminSeed.Email.Trim().ToUpperInvariant(),
                NormalizedUserName = _sysAdminSeed.Email.Trim().ToUpperInvariant(),
                TenantId = null,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var passwordHash = new PasswordHasher<ApplicationUser>();
            var password = _sysAdminSeed.Password;
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("SysAdminSeed:Password is required in appsettings.");
            incomingUser.PasswordHash = passwordHash.HashPassword(incomingUser, password);
            await _userManager.CreateAsync(incomingUser);
            await _userManager.AddToRoleAsync(incomingUser, RoleConstants.SysAdmin);
        }

        /// <summary>
        /// Creates the tenant Owner user when in tenant context (new tenant). Uses tenant Email, FirstName, LastName and a fixed default password.
        /// </summary>
        private async Task InitializeTenantOwnerAsync()
        {
            var tenantInfo = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo;
            bool isTenantContext = tenantInfo?.Id != null;
            if (!isTenantContext)
                return;

            if (string.IsNullOrWhiteSpace(tenantInfo!.Email))
                return;

            var existingUser = await _userManager.Users
                .Where(u => u.TenantId == tenantInfo.Id && u.Email == tenantInfo.Email.Trim())
                .SingleOrDefaultAsync();
            if (existingUser != null)
            {
                if (!await _userManager.IsInRoleAsync(existingUser, RoleConstants.Owner))
                    await _userManager.AddToRoleAsync(existingUser, RoleConstants.Owner);
                await CreateUserTenantMappingAsync(tenantInfo.Email.Trim(), tenantInfo.Id);
                return;
            }

            var password = _sysAdminSeed.DefaultTenantOwnerPassword;
            if (string.IsNullOrWhiteSpace(password))
                password = "P@ssw0rd@123";

            var incomingUser = new ApplicationUser
            {
                FirstName = tenantInfo.FirstName ?? string.Empty,
                LastName = tenantInfo.LastName ?? string.Empty,
                Email = tenantInfo.Email.Trim(),
                UserName = tenantInfo.Email.Trim(),
                NormalizedEmail = tenantInfo.Email.Trim().ToUpperInvariant(),
                NormalizedUserName = tenantInfo.Email.Trim().ToUpperInvariant(),
                TenantId = tenantInfo.Id,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var passwordHash = new PasswordHasher<ApplicationUser>();
            incomingUser.PasswordHash = passwordHash.HashPassword(incomingUser, password);
            await _userManager.CreateAsync(incomingUser);
            await _userManager.AddToRoleAsync(incomingUser, RoleConstants.Owner);
            await CreateUserTenantMappingAsync(tenantInfo.Email.Trim(), tenantInfo.Id);
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

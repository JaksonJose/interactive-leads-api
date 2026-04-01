using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Identity.Roles;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public class ApplicationDbSeeder(
        IMultiTenantContextAccessor<InteractiveTenantInfo> tenantInfoContextAccessor,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext,
        RoleSeeder roleSeeder,
        IOptions<SysAdminSeedSettings> sysAdminSeed)
    {
        private const string DefaultInboxName = "Geral";

        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _tenantInfoContextAccessor = tenantInfoContextAccessor;
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _applicationDbContext = applicationDbContext;
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
                    var tenantInfo = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo;
                    // Global context: seed roles once (shared DB). Tenant context with isolated DB: seed roles so Owner/Manager/Agent exist in tenant DB.
                    await _roleSeeder.SeedRolesAsync(cancellationToken);

                    await InitializeAdminUserAsync();

                    // In tenant-context, ensure tenant bootstrap is atomic (important for shared DB).
                    if (tenantInfo?.Id != null)
                    {
                        var executionStrategy = _applicationDbContext.Database.CreateExecutionStrategy();
                        await executionStrategy.ExecuteAsync(async () =>
                        {
                            await using var tx = await _applicationDbContext.Database.BeginTransactionAsync(cancellationToken);
                            await InitializeTenantOwnerAsync();
                            await InitializeCrmDefaultsAsync(cancellationToken);
                            await tx.CommitAsync(cancellationToken);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Creates global SysAdmin when in global context (TenantInfo.Id is null).
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
                return;
            }

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
                IsActive = false
            };

            var passwordHash = new PasswordHasher<ApplicationUser>();
            var temporaryPassword = Guid.NewGuid().ToString("N") + "Aa1!";
            incomingUser.PasswordHash = passwordHash.HashPassword(incomingUser, temporaryPassword);
            await _userManager.CreateAsync(incomingUser);
            await _userManager.AddToRoleAsync(incomingUser, RoleConstants.Owner);
        }

        private async Task InitializeCrmDefaultsAsync(CancellationToken cancellationToken)
        {
            var tenantInfo = _tenantInfoContextAccessor.MultiTenantContext.TenantInfo;
            bool isTenantContext = tenantInfo?.Id != null;
            if (!isTenantContext)
                return;

            // 1) Ensure Crm.Tenant exists (Identifier == Finbuckle tenant identifier).
            var crmTenant = await _applicationDbContext.Tenants
                .FirstOrDefaultAsync(t => t.Identifier == tenantInfo!.Id, cancellationToken);

            if (crmTenant == null)
            {
                crmTenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = tenantInfo!.Name ?? string.Empty,
                    Identifier = tenantInfo.Id
                };
                _applicationDbContext.Tenants.Add(crmTenant);
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
            }

            // 2) Ensure a default Company exists for this tenant.
            var company = await _applicationDbContext.Companies
                .FirstOrDefaultAsync(c => c.TenantId == crmTenant.Id, cancellationToken);

            if (company == null)
            {
                company = new Company
                {
                    Id = Guid.NewGuid(),
                    TenantId = crmTenant.Id,
                    Name = tenantInfo!.Name ?? string.Empty
                };
                _applicationDbContext.Companies.Add(company);
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
            }

            // 3) Ensure one default active Inbox exists for the company.
            var hasAnyInbox = await _applicationDbContext.Inboxes
                .AnyAsync(i => i.CompanyId == company.Id, cancellationToken);

            if (!hasAnyInbox)
            {
                _applicationDbContext.Inboxes.Add(new Inbox
                {
                    Id = Guid.NewGuid(),
                    CompanyId = company.Id,
                    Name = DefaultInboxName,
                    IsActive = true
                });
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
            }

            // 4) Example teams for new companies (idempotent per company).
            var hasTeams = await _applicationDbContext.Teams
                .AnyAsync(t => t.CompanyId == company.Id, cancellationToken);

            if (!hasTeams)
            {
                var now = DateTimeOffset.UtcNow;
                _applicationDbContext.Teams.AddRange(
                    new Team
                    {
                        Id = Guid.NewGuid(),
                        TenantId = crmTenant.Id,
                        CompanyId = company.Id,
                        Name = "Sales",
                        IsActive = true,
                        CreatedAt = now
                    },
                    new Team
                    {
                        Id = Guid.NewGuid(),
                        TenantId = crmTenant.Id,
                        CompanyId = company.Id,
                        Name = "Support",
                        IsActive = true,
                        CreatedAt = now
                    });
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

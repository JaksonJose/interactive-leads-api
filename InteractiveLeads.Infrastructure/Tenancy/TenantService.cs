using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Tenancy;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Models;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace InteractiveLeads.Infrastructure.Tenancy
{
    public class TenantService : ITenantService
    {
        private readonly IMultiTenantStore<InteractiveTenantInfo> _tenantStore;
        private readonly TenantDbContext _tenantDbContext;
        private readonly ApplicationDbSeeder _dbSeeder;
        private readonly IServiceProvider _serviceProvider;

        public TenantService(
            IMultiTenantStore<InteractiveTenantInfo> tenantStore, 
            TenantDbContext tenantDbContext,
            ApplicationDbSeeder dbSeeder, 
            IServiceProvider serviceProvider)
        {
            _dbSeeder = dbSeeder;
            _tenantStore = tenantStore;
            _tenantDbContext = tenantDbContext;
            _serviceProvider = serviceProvider;
        }

        public async Task<ResultResponse> ActivateAsync(string id, CancellationToken ct = default)
        {
            // Block operations on root tenant
            if (id == TenancyConstants.Root.Id)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Cannot modify root tenant", "tenant.root_modification_denied");
                return errorResponse;
            }

            var tenantInDb = await _tenantStore.TryGetAsync(id);
            tenantInDb.IsActive = true;

            await _tenantStore.TryUpdateAsync(tenantInDb);

            var response = new ResultResponse();
            response.AddSuccessMessage("Tenant activated successfully", "tenant.activated_successfully");
            return response;
        }

        public async Task<ResultResponse> CreateTenantAsync(CreateTenantRequest createTenantRequest, CancellationToken ct)
        {
            // Generate unique identifier based on company name
            string uniqueIdentifier = await GenerateUniqueTenantIdentifier(createTenantRequest.Name);

            var newTenant = new InteractiveTenantInfo
            {
                Id = uniqueIdentifier,
                Name = createTenantRequest.Name,
                Identifier = uniqueIdentifier,
                IsActive = createTenantRequest.IsActive,
                ConnectionString = createTenantRequest.ConnectionString,
                Email = createTenantRequest.Email,
                FirstName = createTenantRequest.FirstName,
                LastName = createTenantRequest.LastName,
                ExpirationDate = createTenantRequest.ExpirationDate
            };

            bool isSuccess = await _tenantStore.TryAddAsync(newTenant);
            if (!isSuccess)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Failed to create tenant. Identifier may already exist.");
                return errorResponse;
            }

            // Seeding tenant data
            await using var scope = _serviceProvider.CreateAsyncScope();

            _serviceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<InteractiveTenantInfo>()
                {
                    TenantInfo = newTenant
                };

            
            await scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>()
                .InitializeDatabaseAsync(ct);

            var response = new ResultResponse();
            response.AddSuccessMessage("Tenant created successfully", "tenant.created_successfully");
            return response;
        }

        public async Task<ResultResponse> DeactivateAsync(string id, CancellationToken ct = default)
        {
            // Block operations on root tenant
            if (id == TenancyConstants.Root.Id)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Cannot modify root tenant", "tenant.root_modification_denied");
                return errorResponse;
            }

            var tenantInDb = await _tenantStore.TryGetAsync(id);
            tenantInDb.IsActive = false;

            await _tenantStore.TryUpdateAsync(tenantInDb);

            var response = new ResultResponse();
            response.AddSuccessMessage("Tenant deactivated successfully", "tenant.deactivated_successfully");
            return response;
        }

        public async Task<ListResponse<TenantResponse>> GetTenantsAsync(PaginationRequest pagination, CancellationToken ct)
        {
            if (!pagination.IsValid())
            {
                pagination = new PaginationRequest();
            }

            var totalTenants = await _tenantDbContext.TenantInfo
                .Where(t => t.Identifier != TenancyConstants.Root.Id)
                .CountAsync(ct);

            var paginatedTenants = await _tenantDbContext.TenantInfo
                .Where(t => t.Identifier != TenancyConstants.Root.Id)
                .OrderBy(t => t.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(ct);
            
            var tenants = paginatedTenants.Adapt<List<TenantResponse>>();

            var response = new ListResponse<TenantResponse>(tenants, totalTenants);
            response.AddSuccessMessage("Tenants retrieved successfully", "tenants.retrieved_successfully");
            return response;
        }

        public async Task<SingleResponse<TenantResponse>> GetTenantsByIdAsync(string id, CancellationToken ct)
        {
            if (id == TenancyConstants.Root.Id)
            {
                ResultResponse errorResponse = new();
                errorResponse.AddErrorMessage("Access to root tenant is not allowed", "tenant.root_access_denied");

                throw new UnauthorizedException(errorResponse);
            }

            var tenantInDb = await _tenantStore.TryGetAsync(id) ?? throw new NotFoundException();

            var newTenant = new InteractiveTenantInfo
            {
                Id = tenantInDb.Identifier,
                Identifier = tenantInDb.Identifier,
                IsActive = tenantInDb.IsActive,
                ConnectionString = tenantInDb.ConnectionString,
                Email = tenantInDb.Email,
                FirstName = tenantInDb.FirstName,
                LastName = tenantInDb.LastName,
                ExpirationDate = tenantInDb.ExpirationDate
            };

            var tenantResponse = tenantInDb.Adapt<TenantResponse>();
            
            var response = new SingleResponse<TenantResponse>(tenantResponse);

            return response;
        }

        public async Task<ResultResponse> UpdateTenantAsync(UpdateTenantRequest updateTenantRequest, CancellationToken ct = default)
        {
            // Block operations on root tenant
            if (updateTenantRequest.Identifier == TenancyConstants.Root.Id)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Cannot modify root tenant", "tenant.root_modification_denied");
                return errorResponse;
            }

            var tenantInDb = await _tenantStore.TryGetAsync(updateTenantRequest.Identifier);
            if (tenantInDb == null)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Tenant not found", "tenant.not_found");
                return errorResponse;
            }

            // Update tenant properties
            tenantInDb.Name = updateTenantRequest.Name;
            tenantInDb.Email = updateTenantRequest.Email;
            tenantInDb.FirstName = updateTenantRequest.FirstName;
            tenantInDb.LastName = updateTenantRequest.LastName;
            tenantInDb.ExpirationDate = updateTenantRequest.ExpirationDate;
            tenantInDb.IsActive = updateTenantRequest.IsActive;
            
            if (!string.IsNullOrWhiteSpace(updateTenantRequest.ConnectionString))
            {
                tenantInDb.ConnectionString = updateTenantRequest.ConnectionString;
            }

            await _tenantStore.TryUpdateAsync(tenantInDb);

            var response = new ResultResponse();
            response.AddSuccessMessage("Tenant updated successfully", "tenant.updated_successfully");
            return response;
        }

        public async Task<ResultResponse> UpdateSubscriptionAsync(UpdateTenantSubscriptionRequest updateTenantSubscriptionRequest, CancellationToken ct = default)
        {
            var tenantInDb = await _tenantStore.TryGetAsync(updateTenantSubscriptionRequest.TenantId);

            tenantInDb.ExpirationDate = updateTenantSubscriptionRequest.NewExpirationDate;

            await _tenantStore.TryUpdateAsync(tenantInDb);

            var response = new ResultResponse();
            response.AddSuccessMessage("Tenant subscription updated successfully", "tenant.subscription_updated_successfully");
            return response;
        }

        /// <summary>
        /// Generates a unique identifier for the tenant based on the company name.
        /// Combines the name slug with a unique identifier (GUID or number) to ensure uniqueness.
        /// </summary>
        /// <param name="companyName">Company name</param>
        /// <returns>Unique identifier for the tenant</returns>
        private async Task<string> GenerateUniqueTenantIdentifier(string companyName)
        {
            // Generate slug based on company name
            string baseSlug = GenerateSlugFromCompanyName(companyName);

            return await GenerateUniqueIdentifier(baseSlug);
        }

        /// <summary>
        /// Converts the company name into a valid slug for identifier.
        /// Takes only the first word of the company name for simplicity.
        /// </summary>
        /// <param name="companyName">Company name</param>
        /// <returns>Slug generated from the first word</returns>
        private static string GenerateSlugFromCompanyName(string companyName)
        {
            // Get the first word of the company name
            string firstWord = companyName
                .ToLowerInvariant()
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .First();

            // Clean up the first word - keep only letters and numbers
            string cleaned = Regex.Replace(firstWord, @"[^a-z0-9]", "");
            
            // Limit size to 20 characters (leaving room for timestamp)
            return cleaned.Length > 20 ? cleaned.Substring(0, 20) : cleaned;
        }

        /// <summary>
        /// Generates a unique identifier by adding a Unix timestamp suffix to the base slug.
        /// Uses milliseconds for maximum uniqueness and collision avoidance.
        /// </summary>
        /// <param name="baseSlug">Base slug</param>
        /// <returns>Unique identifier</returns>
        private async Task<string> GenerateUniqueIdentifier(string baseSlug)
        {
            // Use Unix timestamp in milliseconds for maximum uniqueness
            long unixTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string identifierWithTimestamp = $"{baseSlug}-{unixTimestampMs}";
            
            // Verify uniqueness (practically impossible to conflict with millisecond precision)
            var existingTenant = await _tenantStore.TryGetAsync(identifierWithTimestamp);
            if (existingTenant == null)
            {
                return identifierWithTimestamp;
            }

            // If somehow there's a conflict (extremely rare), add a small random number
            // This should never happen with millisecond precision, but provides extra safety
            int randomSuffix = new Random().Next(100, 999);
            return $"{baseSlug}-{unixTimestampMs}-{randomSuffix}";
        }
    }
}

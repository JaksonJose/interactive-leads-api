using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Constants;
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
using Microsoft.Extensions.Options;
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

        private static bool IsGlobalTenantId(string? id) => string.IsNullOrEmpty(id) || id == TenancyConstants.GlobalTenantIdentifier;

        public async Task<ResultResponse> ActivateAsync(string id, CancellationToken ct = default)
        {
            if (IsGlobalTenantId(id))
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Cannot modify global context", "tenant.global_modification_denied");
                return errorResponse;
            }

            var tenantInDb = await _tenantStore.TryGetAsync(id);
            if (tenantInDb == null)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Tenant not found", "tenant.not_found");
                return errorResponse;
            }
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

            // Create active subscription for the new tenant (default plan, one active per tenant)
            var defaultPrice = await BillingSeed.GetDefaultPlanPriceAsync(_tenantDbContext, ct);
            if (defaultPrice != null)
            {
                var now = DateTime.UtcNow;
                _tenantDbContext.Subscriptions.Add(new Subscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = uniqueIdentifier,
                    PlanId = defaultPrice.PlanId,
                    PlanPriceId = defaultPrice.Id,
                    Status = SubscriptionStatus.Active,
                    StartDate = now,
                    EndDate = createTenantRequest.ExpirationDate,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await _tenantDbContext.SaveChangesAsync(ct);
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
            if (IsGlobalTenantId(id))
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Cannot modify global context", "tenant.global_modification_denied");
                return errorResponse;
            }

            var tenantInDb = await _tenantStore.TryGetAsync(id);
            if (tenantInDb == null)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Tenant not found", "tenant.not_found");
                return errorResponse;
            }
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

            var totalTenants = await _tenantDbContext.TenantInfo.CountAsync(ct);

            var paginatedTenants = await _tenantDbContext.TenantInfo
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
            if (IsGlobalTenantId(id))
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Access to global context is not allowed", "tenant.global_access_denied");
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
            if (IsGlobalTenantId(updateTenantRequest.Identifier))
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Cannot modify global context", "tenant.global_modification_denied");
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
            if (tenantInDb == null)
            {
                var errorResponse = new ResultResponse();
                errorResponse.AddErrorMessage("Tenant not found", "tenant.not_found");
                return errorResponse;
            }

            tenantInDb.ExpirationDate = updateTenantSubscriptionRequest.NewExpirationDate;
            await _tenantStore.TryUpdateAsync(tenantInDb);

            // Keep Subscription entity in sync (source of truth for access control)
            var activeSub = await _tenantDbContext.Subscriptions
                .FirstOrDefaultAsync(s => s.TenantId == updateTenantSubscriptionRequest.TenantId && s.Status == SubscriptionStatus.Active, ct);
            if (activeSub != null)
            {
                activeSub.EndDate = updateTenantSubscriptionRequest.NewExpirationDate;
                activeSub.UpdatedAt = DateTime.UtcNow;
                await _tenantDbContext.SaveChangesAsync(ct);
            }
            else
            {
                // No active subscription: create one with default plan (e.g. admin extending subscription)
                var defaultPrice = await BillingSeed.GetDefaultPlanPriceAsync(_tenantDbContext, ct);
                if (defaultPrice != null)
                {
                    var now = DateTime.UtcNow;
                    _tenantDbContext.Subscriptions.Add(new Subscription
                    {
                        Id = Guid.NewGuid(),
                        TenantId = updateTenantSubscriptionRequest.TenantId,
                        PlanId = defaultPrice.PlanId,
                        PlanPriceId = defaultPrice.Id,
                        Status = SubscriptionStatus.Active,
                        StartDate = now,
                        EndDate = updateTenantSubscriptionRequest.NewExpirationDate,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    await _tenantDbContext.SaveChangesAsync(ct);
                }
            }

            var response = new ResultResponse();
            response.AddSuccessMessage("Tenant subscription updated successfully", "tenant.subscription_updated_successfully");
            return response;
        }

        public async Task<ListResponse<PlanResponse>> GetPlansAsync(bool includeLimitsAndFeatures, CancellationToken ct = default)
        {
            var plans = await _tenantDbContext.Plans
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            var subscriptionPlanService = _serviceProvider.GetRequiredService<ISubscriptionPlanService>();
            var list = new List<PlanResponse>();
            foreach (var p in plans)
            {
                var resp = new PlanResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Identifier = p.Identifier,
                    IsActive = p.IsActive
                };
                if (includeLimitsAndFeatures)
                {
                    resp.Limits = await subscriptionPlanService.GetPlanLimitsAsync(p.Id, ct);
                    resp.Features = (await subscriptionPlanService.GetPlanFeaturesAsync(p.Id, ct)).ToList();
                }
                list.Add(resp);
            }
            var response = new ListResponse<PlanResponse>(list, list.Count);
            response.AddSuccessMessage("Plans retrieved successfully", "plans.retrieved_successfully");
            return response;
        }

        public async Task<SingleResponse<PlanResponse>> GetPlanByIdAsync(Guid planId, bool includePrices = false, CancellationToken ct = default)
        {
            var plan = await _tenantDbContext.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, ct);
            if (plan == null)
                throw new NotFoundException();

            var subscriptionPlanService = _serviceProvider.GetRequiredService<ISubscriptionPlanService>();
            var limits = await subscriptionPlanService.GetPlanLimitsAsync(plan.Id, ct);
            var features = (await subscriptionPlanService.GetPlanFeaturesAsync(plan.Id, ct)).ToList();

            IReadOnlyList<PlanPriceResponse>? prices = null;
            if (includePrices)
            {
                var priceList = await _tenantDbContext.PlanPrices
                    .AsNoTracking()
                    .Where(pp => pp.PlanId == plan.Id)
                    .OrderBy(pp => pp.BillingInterval).ThenBy(pp => pp.IntervalCount)
                    .Select(pp => new PlanPriceResponse
                    {
                        Id = pp.Id,
                        PlanId = pp.PlanId,
                        Price = pp.Price,
                        Currency = pp.Currency,
                        BillingInterval = (int)pp.BillingInterval,
                        IntervalCount = pp.IntervalCount,
                        IsActive = pp.IsActive
                    })
                    .ToListAsync(ct);
                prices = priceList;
            }

            var data = new PlanResponse
            {
                Id = plan.Id,
                Name = plan.Name,
                Identifier = plan.Identifier,
                IsActive = plan.IsActive,
                Limits = limits,
                Features = features,
                Prices = prices
            };
            var response = new SingleResponse<PlanResponse>(data);
            response.AddSuccessMessage("Plan retrieved successfully", "plan.retrieved_successfully");
            return response;
        }

        public async Task<ListResponse<PlanPriceResponse>> GetPlanPricesAsync(Guid planId, CancellationToken ct = default)
        {
            var planExists = await _tenantDbContext.Plans.AnyAsync(p => p.Id == planId, ct);
            if (!planExists)
                throw new NotFoundException();

            var list = await _tenantDbContext.PlanPrices
                .AsNoTracking()
                .Where(pp => pp.PlanId == planId)
                .OrderBy(pp => pp.BillingInterval).ThenBy(pp => pp.IntervalCount)
                .Select(pp => new PlanPriceResponse
                {
                    Id = pp.Id,
                    PlanId = pp.PlanId,
                    Price = pp.Price,
                    Currency = pp.Currency,
                    BillingInterval = (int)pp.BillingInterval,
                    IntervalCount = pp.IntervalCount,
                    IsActive = pp.IsActive
                })
                .ToListAsync(ct);
            var response = new ListResponse<PlanPriceResponse>(list, list.Count);
            response.AddSuccessMessage("Plan prices retrieved successfully", "plan_prices.retrieved_successfully");
            return response;
        }

        public async Task<SingleResponse<PlanPriceResponse>> CreatePlanPriceAsync(Guid planId, CreatePlanPriceRequest request, CancellationToken ct = default)
        {
            var plan = await _tenantDbContext.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
            if (plan == null)
                throw new NotFoundException();

            if (request.BillingInterval is not 0 and not 1)
            {
                var err = new ResultResponse();
                err.AddErrorMessage("BillingInterval must be 0 (Month) or 1 (Year).", "plan_price.invalid_interval");
                throw new BadRequestException(err);
            }

            var now = DateTime.UtcNow;
            var planPrice = new PlanPrice
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                Price = request.Price,
                Currency = request.Currency?.Trim().ToUpperInvariant() ?? "BRL",
                BillingInterval = (BillingInterval)request.BillingInterval,
                IntervalCount = request.IntervalCount,
                IsActive = request.IsActive,
                CreatedAt = now
            };
            _tenantDbContext.PlanPrices.Add(planPrice);
            await _tenantDbContext.SaveChangesAsync(ct);

            var data = new PlanPriceResponse
            {
                Id = planPrice.Id,
                PlanId = planPrice.PlanId,
                Price = planPrice.Price,
                Currency = planPrice.Currency,
                BillingInterval = (int)planPrice.BillingInterval,
                IntervalCount = planPrice.IntervalCount,
                IsActive = planPrice.IsActive
            };
            var response = new SingleResponse<PlanPriceResponse>(data);
            response.AddSuccessMessage("Plan price created successfully", "plan_price.created_successfully");
            return response;
        }

        public async Task<SingleResponse<PlanPriceResponse>> UpdatePlanPriceAsync(Guid planId, Guid priceId, UpdatePlanPriceRequest request, CancellationToken ct = default)
        {
            var planPrice = await _tenantDbContext.PlanPrices.FirstOrDefaultAsync(pp => pp.Id == priceId && pp.PlanId == planId, ct);
            if (planPrice == null)
                throw new NotFoundException();

            if (request.Price.HasValue)
                planPrice.Price = request.Price.Value;
            if (request.Currency != null)
                planPrice.Currency = request.Currency.Trim().ToUpperInvariant();
            if (request.BillingInterval.HasValue)
            {
                if (request.BillingInterval.Value is not 0 and not 1)
                {
                    var err = new ResultResponse();
                    err.AddErrorMessage("BillingInterval must be 0 (Month) or 1 (Year).", "plan_price.invalid_interval");
                    throw new BadRequestException(err);
                }
                planPrice.BillingInterval = (BillingInterval)request.BillingInterval.Value;
            }
            if (request.IntervalCount.HasValue)
                planPrice.IntervalCount = request.IntervalCount.Value;
            if (request.IsActive.HasValue)
                planPrice.IsActive = request.IsActive.Value;

            await _tenantDbContext.SaveChangesAsync(ct);

            var data = new PlanPriceResponse
            {
                Id = planPrice.Id,
                PlanId = planPrice.PlanId,
                Price = planPrice.Price,
                Currency = planPrice.Currency,
                BillingInterval = (int)planPrice.BillingInterval,
                IntervalCount = planPrice.IntervalCount,
                IsActive = planPrice.IsActive
            };
            var response = new SingleResponse<PlanPriceResponse>(data);
            response.AddSuccessMessage("Plan price updated successfully", "plan_price.updated_successfully");
            return response;
        }

        public async Task<SingleResponse<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken ct = default)
        {
            var identifierExists = await _tenantDbContext.Plans.AnyAsync(p => p.Identifier == request.Identifier, ct);
            if (identifierExists)
            {
                var err = new ResultResponse();
                err.AddErrorMessage("A plan with this identifier already exists.", ErrorKeys.PLAN_IDENTIFIER_ALREADY_EXISTS);
                throw new ConflictException(err);
            }

            var now = DateTime.UtcNow;
            var plan = new Plan
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Identifier = request.Identifier.Trim().ToLowerInvariant(),
                IsActive = request.IsActive,
                CreatedAt = now
            };
            _tenantDbContext.Plans.Add(plan);

            if (request.Limits != null && request.Limits.Count > 0)
            {
                foreach (var kv in request.Limits)
                {
                    _tenantDbContext.PlanLimits.Add(new PlanLimit
                    {
                        Id = Guid.NewGuid(),
                        PlanId = plan.Id,
                        LimitKey = kv.Key,
                        LimitValue = kv.Value
                    });
                }
            }
            if (request.Features != null && request.Features.Count > 0)
            {
                foreach (var key in request.Features.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    _tenantDbContext.PlanFeatures.Add(new PlanFeature
                    {
                        Id = Guid.NewGuid(),
                        PlanId = plan.Id,
                        FeatureKey = key.Trim()
                    });
                }
            }

            await _tenantDbContext.SaveChangesAsync(ct);

            var subscriptionPlanService = _serviceProvider.GetRequiredService<ISubscriptionPlanService>();
            var limits = await subscriptionPlanService.GetPlanLimitsAsync(plan.Id, ct);
            var features = (await subscriptionPlanService.GetPlanFeaturesAsync(plan.Id, ct)).ToList();
            var data = new PlanResponse
            {
                Id = plan.Id,
                Name = plan.Name,
                Identifier = plan.Identifier,
                IsActive = plan.IsActive,
                Limits = limits,
                Features = features
            };
            var response = new SingleResponse<PlanResponse>(data);
            response.AddSuccessMessage("Plan created successfully", "plan.created_successfully");
            return response;
        }

        public async Task<SingleResponse<PlanResponse>> UpdatePlanAsync(Guid planId, UpdatePlanRequest request, CancellationToken ct = default)
        {
            var plan = await _tenantDbContext.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
            if (plan == null)
                throw new NotFoundException();

            if (request.Name != null)
                plan.Name = request.Name;
            if (request.Identifier != null)
            {
                var identifierExists = await _tenantDbContext.Plans.AnyAsync(p => p.Identifier == request.Identifier.Trim().ToLowerInvariant() && p.Id != planId, ct);
                if (identifierExists)
                {
                    var err = new ResultResponse();
                    err.AddErrorMessage("A plan with this identifier already exists.", ErrorKeys.PLAN_IDENTIFIER_ALREADY_EXISTS);
                    throw new ConflictException(err);
                }
                plan.Identifier = request.Identifier.Trim().ToLowerInvariant();
            }
            if (request.IsActive.HasValue)
                plan.IsActive = request.IsActive.Value;

            if (request.Limits != null)
            {
                var existingLimits = await _tenantDbContext.PlanLimits.Where(l => l.PlanId == planId).ToListAsync(ct);
                _tenantDbContext.PlanLimits.RemoveRange(existingLimits);
                foreach (var kv in request.Limits)
                {
                    _tenantDbContext.PlanLimits.Add(new PlanLimit
                    {
                        Id = Guid.NewGuid(),
                        PlanId = plan.Id,
                        LimitKey = kv.Key,
                        LimitValue = kv.Value
                    });
                }
            }
            if (request.Features != null)
            {
                var existingFeatures = await _tenantDbContext.PlanFeatures.Where(f => f.PlanId == planId).ToListAsync(ct);
                _tenantDbContext.PlanFeatures.RemoveRange(existingFeatures);
                foreach (var key in request.Features.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    _tenantDbContext.PlanFeatures.Add(new PlanFeature
                    {
                        Id = Guid.NewGuid(),
                        PlanId = plan.Id,
                        FeatureKey = key.Trim()
                    });
                }
            }

            await _tenantDbContext.SaveChangesAsync(ct);

            var subscriptionPlanService = _serviceProvider.GetRequiredService<ISubscriptionPlanService>();
            var limits = await subscriptionPlanService.GetPlanLimitsAsync(plan.Id, ct);
            var features = (await subscriptionPlanService.GetPlanFeaturesAsync(plan.Id, ct)).ToList();
            var data = new PlanResponse
            {
                Id = plan.Id,
                Name = plan.Name,
                Identifier = plan.Identifier,
                IsActive = plan.IsActive,
                Limits = limits,
                Features = features
            };
            var response = new SingleResponse<PlanResponse>(data);
            response.AddSuccessMessage("Plan updated successfully", "plan.updated_successfully");
            return response;
        }

        public async Task<ResultResponse> AssignSubscriptionAsync(AssignTenantSubscriptionRequest request, CancellationToken ct = default)
        {
            if (IsGlobalTenantId(request.TenantId))
            {
                var err = new ResultResponse();
                err.AddErrorMessage("Cannot assign subscription for global context", "tenant.global_modification_denied");
                return err;
            }

            var tenantInDb = await _tenantStore.TryGetAsync(request.TenantId);
            if (tenantInDb == null)
            {
                var err = new ResultResponse();
                err.AddErrorMessage("Tenant not found", "tenant.not_found");
                return err;
            }

            var planPrice = await _tenantDbContext.PlanPrices
                .AsNoTracking()
                .Include(pp => pp.Plan)
                .FirstOrDefaultAsync(pp => pp.Id == request.PlanPriceId, ct);
            if (planPrice == null || !planPrice.IsActive || planPrice.Plan == null || !planPrice.Plan.IsActive)
            {
                var err = new ResultResponse();
                err.AddErrorMessage("Plan price not found or inactive", "plan.not_found");
                return err;
            }

            var now = DateTime.UtcNow;
            // End any other active subscription for this tenant (at most one active per tenant)
            var otherActive = await _tenantDbContext.Subscriptions
                .Where(s => s.TenantId == request.TenantId && s.Status == SubscriptionStatus.Active)
                .ToListAsync(ct);
            foreach (var sub in otherActive)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.UpdatedAt = now;
            }

            _tenantDbContext.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                PlanId = planPrice.PlanId,
                PlanPriceId = planPrice.Id,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                EndDate = request.EndDate,
                CreatedAt = now,
                UpdatedAt = now
            });

            tenantInDb.ExpirationDate = request.EndDate;
            await _tenantStore.TryUpdateAsync(tenantInDb);
            await _tenantDbContext.SaveChangesAsync(ct);

            var response = new ResultResponse();
            response.AddSuccessMessage("Subscription assigned successfully", "subscription.assigned_successfully");
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

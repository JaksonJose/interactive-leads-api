using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Models;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.Tenancy
{
    /// <summary>
    /// Service for handling cross-tenant operations.
    /// </summary>
    /// <remarks>
    /// Provides functionality to execute operations in different tenant contexts
    /// while maintaining proper isolation and authorization.
    /// </remarks>
    public class CrossTenantService : ICrossTenantService
    {
        private readonly ICrossTenantAuthorizationService _authService;
        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _tenantContextAccessor;
        private readonly IMultiTenantContextSetter _tenantContextSetter;
        private readonly ITenantService _tenantService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CrossTenantService> _logger;

        /// <summary>
        /// Initializes a new instance of the CrossTenantService class.
        /// </summary>
        /// <param name="authService">The authorization service for cross-tenant operations.</param>
        /// <param name="tenantContextAccessor">The multi-tenant context accessor.</param>
        /// <param name="tenantContextSetter">The multi-tenant context setter.</param>
        /// <param name="tenantService">The tenant service for tenant operations.</param>
        /// <param name="currentUserService">The current user service.</param>
        /// <param name="serviceScopeFactory">The service scope factory for creating scoped services.</param>
        /// <param name="logger">The logger for cross-tenant operations.</param>
        public CrossTenantService(
            ICrossTenantAuthorizationService authService,
            IMultiTenantContextAccessor<InteractiveTenantInfo> tenantContextAccessor,
            IMultiTenantContextSetter tenantContextSetter,
            ITenantService tenantService,
            ICurrentUserService currentUserService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<CrossTenantService> logger)
        {
            _authService = authService;
            _tenantContextAccessor = tenantContextAccessor;
            _tenantContextSetter = tenantContextSetter;
            _tenantService = tenantService;
            _currentUserService = currentUserService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Executes an operation in the context of a specific tenant.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The result of the operation.</returns>
        public async Task<T> ExecuteInTenantContextAsync<T>(string tenantId, Func<IServiceProvider, Task<T>> operation)
        {
            var currentUserIdString = _currentUserService.GetUserId();
            if (!Guid.TryParse(currentUserIdString, out var currentUserId))
            {
                currentUserId = Guid.Empty;
            }
            
            // Verify if the current user can access this tenant
            if (!await _authService.CanAccessTenantAsync(currentUserId, tenantId))
            {
                throw new ForbiddenException();
            }

            // Store the original tenant context
            var originalTenantContext = _tenantContextAccessor.MultiTenantContext;
            
            try
            {
                // Get the target tenant information
                var tenantResponse = await _tenantService.GetTenantsByIdAsync(tenantId, CancellationToken.None);
                if (tenantResponse.HasAnyErrorMessage || tenantResponse.Data == null)
                {
                    ResultResponse response = new();
                    response.AddErrorMessage($"Tenant with ID '{tenantId}' not found");

                    throw new NotFoundException(response);
                }
                
                var tenantData = tenantResponse.Data;

                // Convert TenantResponse to InteractiveTenantInfo
                var targetTenant = new InteractiveTenantInfo
                {
                    Id = tenantData.Identifier,
                    Identifier = tenantData.Identifier,
                    Name = tenantData.Name,
                    Email = tenantData.Email,
                    FirstName = tenantData.FirstName,
                    LastName = tenantData.LastName,
                    IsActive = tenantData.IsActive,
                    ExpirationDate = tenantData.ExpirationDate,
                    ConnectionString = tenantData.ConnectionString
                };

                // Validate tenant is active and not expired
                if (!targetTenant.IsActive)
                {
                    ResultResponse response = new();
                    response.AddErrorMessage($"Tenant with ID '{tenantId}' is not active.");

                    throw new ForbiddenException(response);
                }

                if (targetTenant.ExpirationDate < DateTime.UtcNow)
                {
                    ResultResponse response = new();
                    response.AddErrorMessage($"Tenant with ID '{tenantId}' has expired.");

                    throw new ForbiddenException();
                }

                // Create a new scope for the target tenant context
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Switch to the target tenant context within the scope
                var scopeTenantContextSetter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
                scopeTenantContextSetter.MultiTenantContext = new MultiTenantContext<InteractiveTenantInfo>
                {
                    TenantInfo = targetTenant
                };

                _logger.LogInformation("Executing cross-tenant operation for user {UserId} from tenant '{OriginalTenantId}' to tenant '{TargetTenantId}'", 
                    currentUserId, originalTenantContext?.TenantInfo?.Id, tenantId);

                // Execute the operation in the new tenant context
                var result = await operation(scope.ServiceProvider);
                
                // Log the successful cross-tenant operation for audit
                await LogCrossTenantOperationAsync(currentUserId, originalTenantContext?.TenantInfo?.Id, tenantId, operation.Method.Name, true);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cross-tenant operation failed for user {UserId} from tenant '{OriginalTenantId}' to tenant '{TargetTenantId}'", 
                    currentUserId, originalTenantContext?.TenantInfo?.Id, tenantId);
                
                // Log the failed operation
                await LogCrossTenantOperationAsync(currentUserId, originalTenantContext?.TenantInfo?.Id, tenantId, operation.Method.Name, false);
                throw;
            }
        }

        /// <summary>
        /// Executes an operation in the context of a specific tenant.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteInTenantContextAsync(string tenantId, Func<IServiceProvider, Task> operation)
        {
            await ExecuteInTenantContextAsync(tenantId, async (serviceProvider) =>
            {
                await operation(serviceProvider);
                return true; // Dummy return value
            });
        }

        /// <summary>
        /// Executes an operation in the context of a specific tenant (legacy method for backward compatibility).
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>The result of the operation.</returns>
        [Obsolete("Use the overload that accepts IServiceProvider parameter for better DbContext management")]
        public async Task<T> ExecuteInTenantContextAsync<T>(string tenantId, Func<Task<T>> operation)
        {
            return await ExecuteInTenantContextAsync(tenantId, async (serviceProvider) =>
            {
                return await operation();
            });
        }

        /// <summary>
        /// Executes an operation in the context of a specific tenant (legacy method for backward compatibility).
        /// </summary>
        /// <param name="tenantId">The ID of the tenant to execute the operation in.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Obsolete("Use the overload that accepts IServiceProvider parameter for better DbContext management")]
        public async Task ExecuteInTenantContextAsync(string tenantId, Func<Task> operation)
        {
            await ExecuteInTenantContextAsync(tenantId, async (serviceProvider) =>
            {
                await operation();
            });
        }

        /// <summary>
        /// Logs a cross-tenant operation for audit purposes.
        /// </summary>
        /// <param name="userId">The ID of the user performing the operation.</param>
        /// <param name="originalTenantId">The original tenant context.</param>
        /// <param name="targetTenantId">The target tenant being accessed.</param>
        /// <param name="operation">The operation being performed.</param>
        /// <param name="result">The result of the operation (success/failure).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LogCrossTenantOperationAsync(Guid userId, string? originalTenantId, string targetTenantId, string operation, bool result = true)
        {
            var logMessage = $"Cross-Tenant Operation: User {userId} from tenant '{originalTenantId}' performed '{operation}' on tenant '{targetTenantId}' - Result: {(result ? "Success" : "Failure")}";
            
            if (result)
            {
                _logger.LogInformation("Cross-tenant operation completed successfully: {LogMessage}", logMessage);
            }
            else
            {
                _logger.LogWarning("Cross-tenant operation failed: {LogMessage}", logMessage);
            }
            
            // TODO: Implement additional logging to:
            // - Database audit table for compliance
            // - External logging service (Serilog, Application Insights, etc.)
            // - Event bus for real-time monitoring
            // - Security information and event management (SIEM) system
            
            await Task.CompletedTask; // Placeholder for async logging implementation
        }
    }
}

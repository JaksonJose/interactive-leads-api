using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using System.Text.Json;

namespace InteractiveLeads.Api.Middleware
{
    /// <summary>
    /// Middleware that validates the current user can access the tenant resolved for the request (e.g. from header).
    /// Runs after Authentication and MultiTenant. Cross-tenant routes are excluded (they pass tenantId in URL/body and validate in handlers).
    /// </summary>
    public class TenantAccessValidationMiddleware
    {
        private const string CrossTenantPathSegment = "CrossTenant";

        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the TenantAccessValidationMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the request pipeline.</param>
        public TenantAccessValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Validates that the authenticated user can access the current tenant context; returns 403 if not.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Skip cross-tenant routes (they validate tenant access in ExecuteInTenantContextAsync)
            if (context.Request.Path.StartsWithSegments("/api/v1/CrossTenant", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Skip if user is not authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            var tenantContextAccessor = context.RequestServices.GetService<IMultiTenantContextAccessor<InteractiveTenantInfo>>();
            var tenantId = tenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;

            // Skip if no tenant resolved (e.g. login endpoint before tenant is set)
            if (string.IsNullOrEmpty(tenantId))
            {
                await _next(context);
                return;
            }

            var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
            var authService = context.RequestServices.GetRequiredService<ICrossTenantAuthorizationService>();

            var userIdString = currentUserService.GetUserId();
            if (!Guid.TryParse(userIdString, out var userId))
            {
                await _next(context);
                return;
            }

            var canAccess = await authService.CanAccessTenantAsync(userId, tenantId);
            if (!canAccess)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var response = new ResultResponse();
                response.AddErrorMessage("You do not have access to this tenant.", "tenant.access_denied");
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
                return;
            }

            await _next(context);
        }
    }
}

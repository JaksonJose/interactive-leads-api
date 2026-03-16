using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace InteractiveLeads.Infrastructure.Tenancy.Strategies
{
    /// <summary>
    /// Resolves tenant at login by reading email from the request body and looking up the user's TenantId in Identity.Users.
    /// Users with TenantId = null (SysAdmin, Support) result in global context.
    /// Uses a separate scope to query the DB so the request-scoped ApplicationDbContext is not created with a null tenant
    /// (which would cause NullReferenceException when TokenService later uses the same context for FindByNameAsync).
    /// </summary>
    public class UserMappingLookupStrategy : IMultiTenantStrategy
    {
        private readonly IServiceProvider _serviceProvider;

        public UserMappingLookupStrategy(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<string?> GetIdentifierAsync(object context)
        {
            if (context is not HttpContext httpContext)
                return null;

            if (!httpContext.Request.Path.Value?.Contains("/token/login", StringComparison.OrdinalIgnoreCase) ?? true)
                return null;

            if (httpContext.Request.Method != HttpMethods.Post)
                return null;

            try
            {
                httpContext.Request.EnableBuffering();
                httpContext.Request.Body.Position = 0;

                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                httpContext.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body))
                    return null;

                using var jsonDoc = JsonDocument.Parse(body);
                string? email = null;

                if (jsonDoc.RootElement.TryGetProperty("userName", out var userNameElement))
                    email = userNameElement.GetString();
                else if (jsonDoc.RootElement.TryGetProperty("email", out var emailElement))
                    email = emailElement.GetString();

                if (string.IsNullOrWhiteSpace(email))
                    return null;

                var normalizedEmail = email.Trim().ToUpperInvariant();

                // Use a new scope so we don't create the request-scoped ApplicationDbContext with null tenant.
                // That context would then be reused by TokenService and cause NRE when the tenant filter is applied.
                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tenantId = await dbContext.Users
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(u => u.NormalizedEmail == normalizedEmail)
                    .Select(u => u.TenantId)
                    .FirstOrDefaultAsync();

                return string.IsNullOrEmpty(tenantId) ? TenancyConstants.GlobalTenantIdentifier : tenantId;
            }
            catch
            {
                return null;
            }
        }
    }
}

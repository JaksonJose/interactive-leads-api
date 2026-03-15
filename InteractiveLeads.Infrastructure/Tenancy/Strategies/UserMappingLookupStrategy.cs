using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Strategies;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InteractiveLeads.Infrastructure.Tenancy.Strategies
{
    /// <summary>
    /// Strategy that uses a user-tenant mapping table for optimal performance.
    /// This provides O(1) lookup time by directly querying the mapping table.
    /// </summary>
    public class UserMappingLookupStrategy : IMultiTenantStrategy
    {
        private readonly TenantDbContext _tenantDbContext;

        public UserMappingLookupStrategy(TenantDbContext tenantDbContext)
        {
            _tenantDbContext = tenantDbContext;
        }

        public async Task<string?> GetIdentifierAsync(object context)
        {
            if (context is not HttpContext httpContext)
                return null;

            // Only for login endpoint
            if (!httpContext.Request.Path.Value?.Contains("/token/login", StringComparison.OrdinalIgnoreCase) ?? true)
                return null;

            // Only for POST requests with JSON body
            if (httpContext.Request.Method != HttpMethods.Post)
                return null;

            try
            {
                // Read email from request body
                httpContext.Request.EnableBuffering();
                httpContext.Request.Body.Position = 0;

                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                httpContext.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body))
                    return null;

                // Extract email from JSON
                using var jsonDoc = JsonDocument.Parse(body);
                string? email = null;

                if (jsonDoc.RootElement.TryGetProperty("userName", out var userNameElement))
                    email = userNameElement.GetString();
                else if (jsonDoc.RootElement.TryGetProperty("email", out var emailElement))
                    email = emailElement.GetString();

                if (string.IsNullOrWhiteSpace(email))
                    return null;

                // MAXIMUM PERFORMANCE: Direct lookup in mapping table
                // O(1) - direct lookup by unique index, scalable for millions of users
                var mapping = await _tenantDbContext.UserTenantMappings
                    .Where(m => m.Email == email && m.IsActive)
                    .Select(m => m.TenantId)
                    .AsNoTracking() // ← Optimization: no tracking for readonly query
                    .FirstOrDefaultAsync();

                return mapping;
            }
            catch
            {
                return null;
            }
        }
    }
}

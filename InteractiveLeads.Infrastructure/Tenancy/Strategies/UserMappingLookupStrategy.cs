using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Context.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InteractiveLeads.Infrastructure.Tenancy.Strategies
{
    /// <summary>
    /// Resolves tenant at login by reading email from the request body and looking up the user's TenantId in Identity.Users.
    /// Users with TenantId = null (SysAdmin, Support) result in global context.
    /// </summary>
    public class UserMappingLookupStrategy : IMultiTenantStrategy
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public UserMappingLookupStrategy(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
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

                // Resolve tenant from Identity.Users (ignore tenant filter so we can find any user by email)
                var tenantId = await _applicationDbContext.Users
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

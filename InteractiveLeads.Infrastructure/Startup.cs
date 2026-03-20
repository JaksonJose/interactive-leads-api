using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using InteractiveLeads.Application;
using InteractiveLeads.Application.Interfaces.HttpRequests;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Configuration;
using InteractiveLeads.Infrastructure.Context.Application;
using InteractiveLeads.Infrastructure.Context.Tenancy;
using InteractiveLeads.Infrastructure.Context.Tenancy.Interfaces;
using InteractiveLeads.Infrastructure.HttpRequests.Authentications;
using InteractiveLeads.Infrastructure.HttpRequests.Handlers;
using InteractiveLeads.Infrastructure.HttpRequests;
using InteractiveLeads.Infrastructure.Identity;
using InteractiveLeads.Infrastructure.Identity.Activation;
using InteractiveLeads.Infrastructure.Identity.Impersonation;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Identity.Roles;
using InteractiveLeads.Infrastructure.Identity.Tokens;
using InteractiveLeads.Infrastructure.Identity.Users;
using InteractiveLeads.Infrastructure.OpenApi;
using InteractiveLeads.Infrastructure.Tenancy;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using InteractiveLeads.Infrastructure.Tenancy.Strategies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using Polly;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace InteractiveLeads.Infrastructure
{
    public static class Startup
    {
        public static async Task AddDatabaseInitializerAsync(this IServiceProvider serviceProvider, CancellationToken ct = default)
        {
            using var scope = serviceProvider.CreateScope();

            await scope.ServiceProvider.GetRequiredService<ITenantDbSeeder>().InitializeDatabaseAsync(ct);
        }

        public static IServiceCollection AddInfraestructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<TenantDbContext>(options =>
            {
                options.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure());
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });
            
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            services.AddMultiTenant<InteractiveTenantInfo>()
                .WithStrategy<UserMappingLookupStrategy>(ServiceLifetime.Scoped)
                .WithHeaderStrategy("tenant")
                .WithStrategy<JwtTenantFallbackStrategy>(ServiceLifetime.Scoped)
                .WithStore<GlobalTenantStoreWrapper>(ServiceLifetime.Scoped);

            services.AddIdentityService();

            services.Configure<SysAdminSeedSettings>(config.GetSection(SysAdminSeedSettings.SectionName));
            services.Configure<ActivationSettings>(config.GetSection(ActivationSettings.SectionName));

            services.AddTransient<ITenantDbSeeder, TenantDbSeeder>();
            services.AddTransient<ApplicationDbSeeder>();
            services.AddTransient<RoleSeeder>();

            // Register application services
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserSummaryLookupService, UserSummaryLookupService>();
            services.AddScoped<IUserLookupService, UserLookupService>();
            services.AddScoped<IImpersonationService, ImpersonationService>();
            services.AddScoped<IActivationTokenRepository, ActivationTokenRepository>();
            services.AddScoped<IActivationTokenLookupRepository, ActivationTokenLookupRepository>();
            services.AddScoped<IIntegrationExternalIdentifierLookupRepository, IntegrationLookupRepository>();
            services.AddScoped<IUserActivationService, UserActivationService>();

            // Register cross-tenant services
            services.AddScoped<ICrossTenantService, CrossTenantService>();
            services.AddScoped<ICrossTenantAuthorizationService, CrossTenantAuthorizationService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IN8nClient, N8nHttpClient>();
            services.AddScoped<IResponseHandler, DefaultResponseHandler>();
            services.AddScoped<IResponseHandlerProvider, ResponseHandlerProvider>();
            services.AddScoped<IExternalApiHttpClientFactory, ExternalApiHttpClientFactory>();

            services.AddExternalApiHttpClients(config);

            services.AddOpenApiDocumentation(config);

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
        {
            services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(bearer =>
            {
                bearer.RequireHttpsMetadata = false;
                bearer.SaveToken = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                };

                bearer.Events = new()
                {
                    OnMessageReceived = context =>
                    {
                        // SignalR sends the JWT as `access_token` query string when using
                        // HubConnectionBuilder.withUrl(..., { accessTokenFactory: ... }).
                        // We need to read that token for hubs requests.
                        var path = context.HttpContext.Request.Path;
                        var accessToken = context.Request.Query["access_token"].ToString();

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs/chat", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenException)
                        {
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";

                                var response = new ResultResponse().AddErrorMessage("Token has expired", "auth.token_expired");
                                var jsonOptions = new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                };
                                var result = JsonSerializer.Serialize(response, jsonOptions);
                                return context.Response.WriteAsync(result);
                            }

                            return Task.CompletedTask;
                        }
                        else
                        {
                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                context.Response.ContentType = "application/json";

                                var response = new ResultResponse().AddErrorMessage("An unhandled error has occurred", "general.something_went_wrong");
                                var jsonOptions = new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                };
                                var result = JsonSerializer.Serialize(response, jsonOptions);
                                return context.Response.WriteAsync(result);
                            }

                            return Task.CompletedTask;
                        }
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            context.Response.ContentType = "application/json";

                            var response = new ResultResponse().AddErrorMessage("You are not authorized", "general.unauthorized");
                            var jsonOptions = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            };
                            var result = JsonSerializer.Serialize(response, jsonOptions);
                            return context.Response.WriteAsync(result);
                        }

                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        context.Response.ContentType = "application/json";

                        var response = new ResultResponse().AddErrorMessage("You are not authorized to access this resource", "general.access_denied");
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        var result = JsonSerializer.Serialize(response, jsonOptions);
                        return context.Response.WriteAsync(result);
                    }
                };
            });

            return services;
        }

        public static JwtSettings GetJwtSettings(this IServiceCollection services, IConfiguration config)
        {
            var jwtSettings = config.GetSection(nameof(JwtSettings));
            services.Configure<JwtSettings>(jwtSettings);

            return jwtSettings.Get<JwtSettings>() ?? new();
        }

        public static IApplicationBuilder UseInfraestructure(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseMultiTenant();
            app.UseAuthorization();
            app.UseOpenApiDocumentation();
            
            return app;
        }

        internal static IServiceCollection AddIdentityService(this IServiceCollection services)
        {
            return services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            }).AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .Services;
        }

        internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration configuration)
        {
            var swaggerSettings = configuration.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>();

            services.AddEndpointsApiExplorer();
            _ = services.AddOpenApiDocument((document, serviceProvider) =>
            {
                document.PostProcess = doc =>
                {
                    doc.Info.Title = swaggerSettings?.Title ?? string.Empty;
                    doc.Info.Description = swaggerSettings?.Description ?? string.Empty;
                    doc.Info.Contact = new OpenApiContact
                    {
                        Name = swaggerSettings?.ContactName ?? string.Empty,
                        Email = swaggerSettings?.ContactEmail ?? string.Empty,
                        Url = swaggerSettings?.ContactUrl ?? string.Empty,
                    };
                    doc.Info.License = new OpenApiLicense
                    {
                        Name = swaggerSettings?.LicenseName ?? string.Empty,
                        Url = swaggerSettings?.LicenseUrl ?? string.Empty,
                    };
                };

                document.AddSecurity(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter your Bearer token to attach it as a header on your requests.",
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                });

                document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor());
                document.OperationProcessors.Add(new SwaggerGlobalAuthProcessor());
                document.OperationProcessors.Add(new SwaggerHeaderAttributeProcessor());
            });

            return services;
        }

        internal static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app)
        {
            app.UseOpenApi();
            app.UseSwaggerUi(options =>
            {
                options.DefaultModelExpandDepth = -1;
                options.DocExpansion = "none";
                options.TagsSorter = "alpha";
            });

            return app;
        }

        private static IServiceCollection AddExternalApiHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            var integrationSection = configuration.GetSection("Integration");
            if (integrationSection?.GetChildren() == null) return services;

            foreach (var child in integrationSection.GetChildren())
            {
                var apiName = child.Key;
                var url = child["Url"];
                if (string.IsNullOrEmpty(url)) continue;

                var baseAddress = new Uri(url.TrimEnd('/') + "/");
                var authType = child["AuthType"]?.Trim();
                var username = child["Username"];
                var password = child["Password"];
                var isBearerLogin = child.GetValue<bool>("BearerLogin")
                    || string.Equals(authType, "BearerLogin", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrEmpty(authType) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password));

                if (isBearerLogin)
                    services.AddHttpClient($"{apiName}.Login", client => client.BaseAddress = baseAddress);

                var clientBuilder = services.AddHttpClient(apiName, client => client.BaseAddress = baseAddress);
                if (isBearerLogin)
                {
                    clientBuilder.AddHttpMessageHandler(sp => new BearerLoginAuthHandler(
                        sp.GetRequiredService<IConfiguration>(),
                        sp.GetRequiredService<IHttpClientFactory>(),
                        sp.GetRequiredService<IResponseHandlerProvider>(),
                        apiName));
                }

                clientBuilder.AddStandardResilienceHandler(ConfigureResilience);
            }

            return services;
        }

        private static void ConfigureResilience(HttpStandardResilienceOptions options)
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);

            // Retry
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;

            // Circuit break
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        }
    }
}

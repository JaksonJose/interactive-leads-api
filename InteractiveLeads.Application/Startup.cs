using FluentValidation;
using InteractiveLeads.Application.Pipelines;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace InteractiveLeads.Application
{
    /// <summary>
    /// Extension methods for configuring application layer services.
    /// </summary>
    /// <remarks>
    /// Provides dependency injection configuration for the application layer
    /// including MediatR for CQRS pattern and FluentValidation for input validation.
    /// </remarks>
    public static class Startup
    {
        /// <summary>
        /// Registers application layer services with the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for method chaining.</returns>
        /// <remarks>
        /// Configures:
        /// - FluentValidation validators from the current assembly
        /// - MediatR command/query handlers from the current assembly
        /// </remarks>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            return services.AddValidatorsFromAssembly(assembly)
                .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBenaviour<,>))
                .AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssemblies(assembly);
                });
        }
    }
}

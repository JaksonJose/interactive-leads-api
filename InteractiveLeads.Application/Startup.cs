using FluentValidation;
using InteractiveLeads.Application.Feature.Chat.Media;
using InteractiveLeads.Application.Feature.Chat.Messages.Services;
using InteractiveLeads.Application.Dispatching;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Integrations.Settings;
using InteractiveLeads.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace InteractiveLeads.Application
{
    /// <summary>
    /// Extension methods for configuring application layer services.
    /// </summary>
    /// <remarks>
    /// Provides dependency injection configuration for the application layer
    /// including FluentValidation for input validation.
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
    /// - application request handlers from the current assembly
        /// </remarks>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<IRequestDispatcher, RequestDispatcher>();

            var handlerInterfaceType = typeof(IApplicationRequestHandler<,>);
            var handlerTypes = assembly
                .GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .Select(i => new { InterfaceType = i, HandlerType = t }));

            foreach (var item in handlerTypes)
            {
                services.AddScoped(item.InterfaceType, item.HandlerType);
            }

            services.AddScoped<IIntegrationSettingsResolver, IntegrationSettingsResolver>();
            services.AddScoped<IIntegrationExternalIdentifierResolver, IntegrationExternalIdentifierResolver>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IConversationMediaUploadService, ConversationMediaUploadService>();

            return services;
        }
    }
}

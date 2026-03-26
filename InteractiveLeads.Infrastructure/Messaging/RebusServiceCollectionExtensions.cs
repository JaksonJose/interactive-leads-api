using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rebus.Config;
using Rebus.RabbitMq;
using Rebus.Routing.TypeBased;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;
using Rebus.ServiceProvider;

namespace InteractiveLeads.Infrastructure.Messaging;

public static class RebusServiceCollectionExtensions
{
    public static IServiceCollection AddInteractiveLeadsRebus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqSettings>()
            .Bind(configuration.GetSection(RabbitMqSettings.SectionName));

        services.AutoRegisterHandlersFromAssemblyOf<MediaProcessingRequestedHandler>();

        var settings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
                       ?? new RabbitMqSettings();

        services.AddRebus(configure =>
        {
            var vhost = settings.VirtualHost?.Trim() ?? "/";
            if (!vhost.StartsWith('/'))
                vhost = "/" + vhost;
            if (vhost == "/")
                vhost = "/%2F";

            var connectionString =
                $"amqp://{Uri.EscapeDataString(settings.Username)}:{Uri.EscapeDataString(settings.Password)}@{settings.Host}:{settings.Port}{vhost}";

            var rabbit = settings.UseQuorumQueues
                ? configure.Transport(t => t
                    .UseRabbitMq(connectionString, inputQueueName: settings.MediaProcessingQueueName)
                    .InputQueueOptions(q => q.AddArgument("x-queue-type", "quorum"))
                    .DefaultQueueOptions(q => q.AddArgument("x-queue-type", "quorum")))
                : configure.Transport(t => t.UseRabbitMq(connectionString, inputQueueName: settings.MediaProcessingQueueName));

            rabbit
                .Serialization(s => s.UseNewtonsoftJson(new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                    o.RetryStrategy($"{settings.MediaProcessingQueueName}.error", 5);
                })
                .Routing(r => r.TypeBased()
                    .Map<MediaProcessingRequested>(settings.MediaProcessingQueueName));

            return configure;
        });

        return services;
    }
}


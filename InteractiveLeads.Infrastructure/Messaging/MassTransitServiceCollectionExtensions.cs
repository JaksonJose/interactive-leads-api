using System.Text.Json;
using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Infrastructure.Configuration;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Messaging;

public static class MassTransitServiceCollectionExtensions
{
    public static IServiceCollection AddInteractiveLeadsMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqSettings>()
            .Bind(configuration.GetSection(RabbitMqSettings.SectionName));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<InboundIntegrationEventConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var settings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

                cfg.Host(
                    settings.Host,
                    settings.Port,
                    settings.VirtualHost,
                    h =>
                    {
                        h.Username(settings.Username);
                        h.Password(settings.Password);
                    });

                cfg.ConfigureJsonSerializerOptions(options =>
                {
                    options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.PropertyNameCaseInsensitive = true;
                    return options;
                });

                cfg.Message<OutboundMessageDispatch>(m => m.SetEntityName(settings.OutboundExchangeName));

                cfg.ReceiveEndpoint(settings.InboundQueueName, e =>
                {
                    // External publishers (n8n, bridges) send plain JSON, not the MassTransit message envelope.
                    e.UseRawJsonDeserializer(
                        RawSerializerOptions.AnyMessageType,
                        isDefault: true,
                        options =>
                        {
                            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                            options.PropertyNameCaseInsensitive = true;
                            return options;
                        });

                    if (settings.UseQuorumQueues && e is IRabbitMqQueueConfigurator inboundQueue)
                        inboundQueue.SetQuorumQueue(null);

                    e.ConfigureConsumer<InboundIntegrationEventConsumer>(context);
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromMilliseconds(200),
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5)));
                });

                cfg.Publish<OutboundMessageDispatch>(p =>
                {
                    p.BindQueue(settings.OutboundExchangeName, settings.OutboundQueueName, bind =>
                    {
                        if (settings.UseQuorumQueues && bind is IRabbitMqQueueConfigurator outboundQueue)
                            outboundQueue.SetQuorumQueue(null);

                        bind.ExchangeType = "fanout";
                    });
                });
            });
        });

        return services;
    }
}

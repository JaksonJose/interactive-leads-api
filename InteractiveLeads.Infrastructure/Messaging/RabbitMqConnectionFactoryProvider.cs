using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InteractiveLeads.Infrastructure.Messaging;

public interface IRabbitMqConnectionFactoryProvider
{
    ConnectionFactory Create();
}

public sealed class RabbitMqConnectionFactoryProvider(IOptions<RabbitMqSettings> options)
    : IRabbitMqConnectionFactoryProvider
{
    public ConnectionFactory Create()
    {
        var settings = options.Value;
        return new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.Username,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
    }
}


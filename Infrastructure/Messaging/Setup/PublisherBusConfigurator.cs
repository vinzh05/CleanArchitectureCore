using Infrastructure.Messaging.Configuration;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.Messaging.Setup
{
    /// <summary>
    /// Configures MassTransit for Publisher role (WebAPI).
    /// Provides fluent API for clean and testable configuration.
    /// </summary>
    public class PublisherBusConfigurator
    {
        private readonly IServiceCollection _services;
        private readonly RabbitMqSettings _settings;

        public PublisherBusConfigurator(IServiceCollection services, RabbitMqSettings settings)
        {
            _services = services;
            _settings = settings;
        }

        /// <summary>
        /// Configure and register MassTransit for publishing messages.
        /// </summary>
        public IServiceCollection Build()
        {
            _services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    ConfigureHost(cfg);
                    ConfigurePublisher(cfg);
                });
            });

            return _services;
        }

        private void ConfigureHost(IRabbitMqBusFactoryConfigurator cfg)
        {
            cfg.Host(_settings.Host, h =>
            {
                h.Username(_settings.Username);
                h.Password(_settings.Password);

                if (_settings.UseSSL)
                {
                    h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                }

                h.Heartbeat(TimeSpan.FromSeconds(_settings.Connection.RequestedHeartbeat));
                h.RequestedConnectionTimeout(TimeSpan.FromSeconds(_settings.Connection.RequestedConnectionTimeout));
            });
        }

        private void ConfigurePublisher(IRabbitMqBusFactoryConfigurator cfg)
        {
            // Publisher doesn't need to declare queues
            // Only publishes to exchanges that are declared by consumers

            cfg.ConfigurePublish(p =>
            {
                p.UseExecute(context =>
                {
                    // Add correlation ID, timestamp, etc.
                    context.Headers.Set("PublishedAt", DateTimeOffset.UtcNow);
                });
            });

            // Configure message topology
            cfg.PublishTopology.BrokerTopologyOptions = PublishBrokerTopologyOptions.MaintainHierarchy;
        }
    }
}

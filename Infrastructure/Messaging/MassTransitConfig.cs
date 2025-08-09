using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;

namespace Ecom.Infrastructure.Messaging
{
    public static class MassTransitConfig
    {
        // Publisher registration (WebApi) - only ensures bus exists and IPublishEndpoint available
        public static void AddMassTransitPublisher(IServiceCollection services, IConfiguration cfg)
        {
            var mqHost = cfg.GetValue<string>("RabbitMq:Host", "rabbitmq");
            var mqUser = cfg.GetValue<string>("RabbitMq:Username", "guest");
            var mqPass = cfg.GetValue<string>("RabbitMq:Password", "guest");

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, rcfg) =>
                {
                    rcfg.Host(mqHost, h =>
                    {
                        h.Username(mqUser);
                        h.Password(mqPass);
                    });

                    // Do not define queues here for publisher
                });
            });
        }

        // Consumer registration (Worker) - will create endpoints from config and allow configurePerEndpoint to attach consumers
        public static void AddMassTransitConsumers(IServiceCollection services, IConfiguration cfg, Action<IBusRegistrationContext, IRabbitMqReceiveEndpointConfigurator, string>? configurePerEndpoint = null)
        {
            var mqHost = cfg.GetValue<string>("RabbitMq:Host", "rabbitmq");
            var mqUser = cfg.GetValue<string>("RabbitMq:Username", "guest");
            var mqPass = cfg.GetValue<string>("RabbitMq:Password", "guest");
            var prefetch = cfg.GetValue<ushort>("RabbitMq:PrefetchCount", 16);
            var retryCount = cfg.GetValue<int>("RabbitMq:Retry:RetryCount", 5);
            var retryInterval = cfg.GetValue<int>("RabbitMq:Retry:IntervalSeconds", 5);

            services.AddMassTransit(x =>
            {
                // Consumers need to be added by caller: x.AddConsumers(typeof(YourConsumerAssembly).Assembly)
                x.UsingRabbitMq((context, rcfg) =>
                {
                    rcfg.Host(mqHost, h =>
                    {
                        h.Username(mqUser);
                        h.Password(mqPass);
                    });

                    var exchanges = cfg.GetSection("RabbitMq:Exchanges").GetChildren();
                    foreach (var ex in exchanges)
                    {
                        var exchangeName = ex.GetValue<string>("Name", ex.Key.ToLowerInvariant());
                        var exchangeType = ex.GetValue<string>("Type", ExchangeType.Topic);
                        var queueName = ex.GetValue<string>("Queue", $"{exchangeName}.queue");
                        var routingKey = ex.GetValue<string>("RoutingKey", "");

                        rcfg.ReceiveEndpoint(queueName, e =>
                        {
                            e.ConfigureConsumeTopology = false;
                            e.PrefetchCount = prefetch;
                            e.UseMessageRetry(r => r.Interval(retryCount, TimeSpan.FromSeconds(retryInterval)));

                            e.Bind(exchangeName, b =>
                            {
                                b.ExchangeType = exchangeType;
                                if (!string.IsNullOrWhiteSpace(routingKey))
                                    b.RoutingKey = routingKey;
                            });

                            configurePerEndpoint?.Invoke(context, e, ex.Key);
                        });
                    }
                });
            });
        }
    }
}

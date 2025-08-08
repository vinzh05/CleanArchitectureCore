using Infrastructure.Consumers.Common;
using Infrastructure.Consumers.Product;
using Infrastructure.Contracts.Product;
using Infrastructure.IntegrationEvents.Product;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public static class MassTransitConfig
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration config)
        {
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                // Đăng ký các consumer
                x.AddConsumersFromNamespaceContaining<ProductCreatedConsumer>();
                x.AddConsumersFromNamespaceContaining<ProductUpdatedConsumer>();
                x.AddConsumersFromNamespaceContaining<ProductDeletedConsumer>();

                // Cấu hình RabbitMQ
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(config["RabbitMq:Host"] ?? "rabbitmq", h =>
                    {
                        h.Username(config["RabbitMq:Username"] ?? "guest");
                        h.Password(config["RabbitMq:Password"] ?? "guest");
                    });

                    //// Fanout Exchange cho Product Created
                    //cfg.ConfigureFanoutEndpoint<ProductCreatedIntegrationEvent>("product-created-fanout");

                    // Topic Exchange cho Product Updated
                    cfg.ConfigureTopicEndpoint<ProductUpdatedIntegrationEvent>("product-updated-topic", "product.#");

                    //// Direct Exchange cho Product Deleted
                    //cfg.ConfigureDirectEndpoint<ProductDeletedIntegrationEvent>("product-deleted-direct", "product.deleted");
                });
            });

            return services;
        }

        private static void ConfigureFanoutEndpoint<T>(this IRabbitMqBusFactoryConfigurator cfg, string exchangeName) where T : class
        {
            cfg.ReceiveEndpoint(exchangeName, e =>
            {
                e.Bind(exchangeName, x =>
                {
                    x.ExchangeType = ExchangeType.Fanout;
                });
                e.Consumer<TConsumer<T>>();
            });
        }

        private static void ConfigureTopicEndpoint<T>(this IRabbitMqBusFactoryConfigurator cfg, string exchangeName, string routingKeyPattern) where T : class
        {
            cfg.ReceiveEndpoint(exchangeName, e =>
            {
                e.Bind(exchangeName, x =>
                {
                    x.ExchangeType = ExchangeType.Topic;
                    x.RoutingKey = routingKeyPattern;
                });
                e.Consumer<TConsumer<T>>();
            });
        }

        private static void ConfigureDirectEndpoint<T>(this IRabbitMqBusFactoryConfigurator cfg, string exchangeName, string routingKey) where T : class
        {
            cfg.ReceiveEndpoint(exchangeName, e =>
            {
                e.Bind(exchangeName, x =>
                {
                    x.ExchangeType = ExchangeType.Direct;
                    x.RoutingKey = routingKey;
                });
                e.Consumer<TConsumer<T>>();
            });
        }
    }
}

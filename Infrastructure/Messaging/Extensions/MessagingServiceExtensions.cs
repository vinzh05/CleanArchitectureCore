using Infrastructure.Messaging.Abstractions;
using Infrastructure.Messaging.Configuration;
using Infrastructure.Messaging.Outbox;
using Infrastructure.Messaging.Setup;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Infrastructure.Messaging.Extensions
{
    /// <summary>
    /// Extension methods for registering RabbitMQ messaging infrastructure.
    /// Provides clean, fluent API for service registration.
    /// </summary>
    public static class MessagingServiceExtensions
    {
        /// <summary>
        /// Add RabbitMQ Publisher configuration for WebAPI.
        /// Registers MassTransit with RabbitMQ for publishing messages only.
        /// </summary>
        public static IServiceCollection AddRabbitMqPublisher(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register settings
            services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
            services.Configure<OutboxSettings>(configuration.GetSection(OutboxSettings.SectionName));

            // Get settings for configurator
            var settings = configuration
                .GetSection(RabbitMqSettings.SectionName)
                .Get<RabbitMqSettings>() ?? new RabbitMqSettings();

            // Validate settings
            ValidateSettings(settings);

            // Register message type registry
            services.AddSingleton<IMessageTypeRegistry, MessageTypeRegistry>();

            // Configure MassTransit for publishing
            new PublisherBusConfigurator(services, settings).Build();

            return services;
        }

        /// <summary>
        /// Add RabbitMQ Consumer configuration for Worker.
        /// Registers MassTransit with RabbitMQ for consuming messages.
        /// </summary>
        public static IServiceCollection AddRabbitMqConsumer(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IRegistrationConfigurator, RabbitMqSettings> configureConsumers)
        {
            // Register settings
            services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

            // Get settings for configurator
            var settings = configuration
                .GetSection(RabbitMqSettings.SectionName)
                .Get<RabbitMqSettings>() ?? new RabbitMqSettings();

            // Validate settings
            ValidateSettings(settings);

            // Configure MassTransit
            services.AddMassTransit(x =>
            {
                // Allow caller to register consumers
                configureConsumers(x, settings);

                // Configure RabbitMQ bus
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(settings.Host, h =>
                    {
                        h.Username(settings.Username);
                        h.Password(settings.Password);

                        if (settings.UseSSL)
                        {
                            h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                        }

                        h.Heartbeat(TimeSpan.FromSeconds(settings.Connection.RequestedHeartbeat));
                        h.RequestedConnectionTimeout(TimeSpan.FromSeconds(settings.Connection.RequestedConnectionTimeout));
                    });

                    // Configure endpoints for each exchange
                    foreach (var exchange in settings.Exchanges)
                    {
                        var exchangeSettings = exchange.Value;

                        cfg.ReceiveEndpoint(exchangeSettings.Queue, endpoint =>
                        {
                            // Basic endpoint configuration
                            endpoint.ConfigureConsumeTopology = false;
                            endpoint.PrefetchCount = settings.PrefetchCount;
                            endpoint.Durable = exchangeSettings.Durable;
                            endpoint.AutoDelete = exchangeSettings.AutoDelete;

                            // Configure retry
                            if (settings.Retry.UseExponentialBackoff)
                            {
                                endpoint.UseMessageRetry(r => r.Exponential(
                                    settings.Retry.RetryCount,
                                    TimeSpan.FromSeconds(settings.Retry.IntervalSeconds),
                                    TimeSpan.FromSeconds(settings.Retry.MaxIntervalSeconds),
                                    TimeSpan.FromSeconds(settings.Retry.IntervalSeconds)));
                            }
                            else
                            {
                                endpoint.UseMessageRetry(r => r.Interval(
                                    settings.Retry.RetryCount,
                                    TimeSpan.FromSeconds(settings.Retry.IntervalSeconds)));
                            }

                            // Bind to exchange
                            endpoint.Bind(exchangeSettings.Name, binding =>
                            {
                                binding.ExchangeType = exchangeSettings.Type;
                                binding.Durable = exchangeSettings.Durable;
                                binding.AutoDelete = exchangeSettings.AutoDelete;

                                if (!string.IsNullOrWhiteSpace(exchangeSettings.RoutingKey))
                                {
                                    binding.RoutingKey = exchangeSettings.RoutingKey;
                                }
                            });

                            // Configure consumers for this endpoint - delegate to context
                            endpoint.ConfigureConsumers(context);
                        });
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// Add Outbox publisher background service.
        /// Processes outbox messages and publishes them to RabbitMQ.
        /// </summary>
        public static IServiceCollection AddOutboxPublisher(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IMessageTypeRegistry>? configureTypeRegistry = null)
        {
            // Register settings
            services.Configure<OutboxSettings>(configuration.GetSection(OutboxSettings.SectionName));

            // Register message type registry if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IMessageTypeRegistry)))
            {
                services.AddSingleton<IMessageTypeRegistry, MessageTypeRegistry>();
            }

            // Configure type registry
            if (configureTypeRegistry != null)
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                var registry = scope.ServiceProvider.GetRequiredService<IMessageTypeRegistry>();
                configureTypeRegistry(registry);
            }

            // Register hosted service
            services.AddHostedService<OutboxPublisherService>();

            return services;
        }

        /// <summary>
        /// Register integration event types from assembly.
        /// </summary>
        public static IServiceCollection RegisterIntegrationEvents(
            this IServiceCollection services,
            Assembly assembly,
            Func<Type, bool>? predicate = null)
        {
            services.AddSingleton(sp =>
            {
                var registry = sp.GetRequiredService<IMessageTypeRegistry>();
                registry.RegisterTypesFromAssembly(assembly, predicate);
                return registry;
            });

            return services;
        }

        private static void ValidateSettings(RabbitMqSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
            {
                throw new ArgumentException("RabbitMQ Host is required");
            }

            if (string.IsNullOrWhiteSpace(settings.Username))
            {
                throw new ArgumentException("RabbitMQ Username is required");
            }

            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                throw new ArgumentException("RabbitMQ Password is required");
            }

            if (settings.Exchanges == null || settings.Exchanges.Count == 0)
            {
                throw new ArgumentException("At least one exchange configuration is required");
            }

            foreach (var exchange in settings.Exchanges)
            {
                if (string.IsNullOrWhiteSpace(exchange.Value.Name))
                {
                    throw new ArgumentException($"Exchange name is required for {exchange.Key}");
                }

                if (string.IsNullOrWhiteSpace(exchange.Value.Queue))
                {
                    throw new ArgumentException($"Queue name is required for exchange {exchange.Key}");
                }
            }
        }
    }

    /// <summary>
    /// Helper extensions for address manipulation.
    /// </summary>
    internal static class AddressExtensions
    {
        public static string? GetQueueName(this Uri? address)
        {
            if (address == null) return null;
            
            var segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.LastOrDefault();
        }
    }
}

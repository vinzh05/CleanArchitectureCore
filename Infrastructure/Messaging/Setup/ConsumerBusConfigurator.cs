using Infrastructure.Messaging.Configuration;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging.Setup
{
    /// <summary>
    /// Configures MassTransit for Consumer role (Worker).
    /// Automatically discovers and configures consumers based on settings.
    /// </summary>
    public class ConsumerBusConfigurator
    {
        private readonly IServiceCollection _services;
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<ConsumerBusConfigurator>? _logger;
        private readonly List<Action<IBusRegistrationContext, IRabbitMqReceiveEndpointConfigurator>> _endpointConfigurators = new();

        public ConsumerBusConfigurator(IServiceCollection services, RabbitMqSettings settings, ILogger<ConsumerBusConfigurator>? logger = null)
        {
            _services = services;
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Add custom endpoint configurator.
        /// </summary>
        public ConsumerBusConfigurator ConfigureEndpoint(Action<IBusRegistrationContext, IRabbitMqReceiveEndpointConfigurator> configurator)
        {
            _endpointConfigurators.Add(configurator);
            return this;
        }

        /// <summary>
        /// Configure and register MassTransit for consuming messages.
        /// </summary>
        public IServiceCollection Build()
        {
            _services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    ConfigureHost(cfg);
                    ConfigureEndpoints(context, cfg);
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

        private void ConfigureEndpoints(IBusRegistrationContext context, IRabbitMqBusFactoryConfigurator cfg)
        {
            foreach (var exchange in _settings.Exchanges)
            {
                var exchangeKey = exchange.Key;
                var exchangeSettings = exchange.Value;

                _logger?.LogInformation(
                    "Configuring endpoint: Queue={Queue}, Exchange={Exchange}, Type={Type}",
                    exchangeSettings.Queue,
                    exchangeSettings.Name,
                    exchangeSettings.Type);

                cfg.ReceiveEndpoint(exchangeSettings.Queue, endpoint =>
                {
                    ConfigureEndpoint(endpoint, exchangeSettings);
                    ConfigureRetry(endpoint);
                    BindToExchange(endpoint, exchangeSettings);
                    
                    // Apply custom configurators
                    foreach (var configurator in _endpointConfigurators)
                    {
                        configurator(context, endpoint);
                    }
                });
            }
        }

        private void ConfigureEndpoint(IRabbitMqReceiveEndpointConfigurator endpoint, ExchangeSettings settings)
        {
            endpoint.ConfigureConsumeTopology = false; // Manual topology control
            endpoint.PrefetchCount = _settings.PrefetchCount;
            endpoint.Durable = settings.Durable;
            endpoint.AutoDelete = settings.AutoDelete;

            if (settings.QueueSettings.Exclusive)
            {
                endpoint.Exclusive = true;
            }

            // Configure dead letter queue if specified
            if (settings.QueueSettings.DeadLetter != null)
            {
                var dlq = settings.QueueSettings.DeadLetter;
                endpoint.SetQueueArgument("x-dead-letter-exchange", dlq.ExchangeName);
                
                if (!string.IsNullOrWhiteSpace(dlq.RoutingKey))
                {
                    endpoint.SetQueueArgument("x-dead-letter-routing-key", dlq.RoutingKey);
                }
            }

            // Apply custom queue arguments
            foreach (var arg in settings.QueueSettings.Arguments)
            {
                endpoint.SetQueueArgument(arg.Key, arg.Value);
            }
        }

        private void ConfigureRetry(IRabbitMqReceiveEndpointConfigurator endpoint)
        {
            var retry = _settings.Retry;

            if (retry.UseExponentialBackoff)
            {
                endpoint.UseMessageRetry(r => r.Exponential(
                    retry.RetryCount,
                    TimeSpan.FromSeconds(retry.IntervalSeconds),
                    TimeSpan.FromSeconds(retry.MaxIntervalSeconds),
                    TimeSpan.FromSeconds(retry.IntervalSeconds)));
            }
            else
            {
                endpoint.UseMessageRetry(r => r.Interval(
                    retry.RetryCount,
                    TimeSpan.FromSeconds(retry.IntervalSeconds)));
            }
        }

        private void BindToExchange(IRabbitMqReceiveEndpointConfigurator endpoint, ExchangeSettings settings)
        {
            endpoint.Bind(settings.Name, binding =>
            {
                binding.ExchangeType = settings.Type;
                binding.Durable = settings.Durable;
                binding.AutoDelete = settings.AutoDelete;

                if (!string.IsNullOrWhiteSpace(settings.RoutingKey))
                {
                    binding.RoutingKey = settings.RoutingKey;
                }
            });
        }
    }
}

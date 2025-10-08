using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Infrastructure.Messaging.Setup
{
    /// <summary>
    /// Discovers and registers consumers automatically based on conventions.
    /// Eliminates manual switch-case consumer registration.
    /// </summary>
    public class ConsumerRegistrar
    {
        private readonly IServiceCollection _services;
        private readonly List<Assembly> _assemblies = new();
        private readonly Dictionary<string, Type> _consumerMappings = new();

        public ConsumerRegistrar(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Add assembly to scan for consumers.
        /// </summary>
        public ConsumerRegistrar AddConsumersFromAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
            return this;
        }

        /// <summary>
        /// Manually map exchange key to consumer type.
        /// Useful for explicit control over routing.
        /// </summary>
        public ConsumerRegistrar MapConsumer<TConsumer>(string exchangeKey) where TConsumer : class, IConsumer
        {
            _consumerMappings[exchangeKey] = typeof(TConsumer);
            return this;
        }

        /// <summary>
        /// Register all consumers with MassTransit.
        /// </summary>
        public void RegisterConsumers(IBusRegistrationConfigurator configurator)
        {
            // Register from assemblies
            foreach (var assembly in _assemblies)
            {
                configurator.AddConsumers(assembly);
            }

            // Register manual mappings
            foreach (var mapping in _consumerMappings)
            {
                RegisterConsumerType(configurator, mapping.Value);
            }
        }

        /// <summary>
        /// Get consumer type for exchange key.
        /// Uses naming convention: ExchangeKey + "Consumer"
        /// Example: "ProductCreated" -> "ProductCreatedConsumer"
        /// </summary>
        public Type? GetConsumerTypeForExchange(string exchangeKey)
        {
            // Check explicit mappings first
            if (_consumerMappings.TryGetValue(exchangeKey, out var mappedType))
            {
                return mappedType;
            }

            // Try convention-based discovery
            var consumerName = $"{exchangeKey}Consumer";
            
            foreach (var assembly in _assemblies)
            {
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == consumerName && typeof(IConsumer).IsAssignableFrom(t));

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// Configure consumer on endpoint based on exchange key.
        /// </summary>
        public void ConfigureConsumerOnEndpoint(
            string exchangeKey,
            IBusRegistrationContext context,
            IRabbitMqReceiveEndpointConfigurator endpoint)
        {
            var consumerType = GetConsumerTypeForExchange(exchangeKey);
            
            if (consumerType == null)
            {
                return;
            }

            // Use reflection to call endpoint.ConfigureConsumer<T>(context)
            var method = endpoint.GetType()
                .GetMethods()
                .FirstOrDefault(m => 
                    m.Name == "ConfigureConsumer" && 
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(IBusRegistrationContext))
                ?.MakeGenericMethod(consumerType);

            method?.Invoke(endpoint, new object[] { context });
        }

        private void RegisterConsumerType(IBusRegistrationConfigurator configurator, Type consumerType)
        {
            // Use reflection to call configurator.AddConsumer<T>()
            var method = configurator.GetType()
                .GetMethods()
                .FirstOrDefault(m => 
                    m.Name == "AddConsumer" && 
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 0)
                ?.MakeGenericMethod(consumerType);

            method?.Invoke(configurator, null);
        }
    }
}

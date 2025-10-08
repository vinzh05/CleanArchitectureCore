using Infrastructure.Consumers.Hikvision;
using Infrastructure.Consumers.Order;
using Infrastructure.Consumers.Product;
using Infrastructure.DI;
using Infrastructure.Messaging.Extensions;

namespace Worker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, cfg) => cfg.AddDefaultConfiguration())
                .ConfigureServices((ctx, services) =>
                {
                    var config = ctx.Configuration;

                    // Infrastructure: Database, Repositories, Caching, JWT
                    // Enable Outbox Publisher for this worker
                    services.AddInfrastructure(config, addOutboxPublisher: true);

                    // Register RabbitMQ consumers
                    services.AddRabbitMqConsumer(config, (cfg, settings) =>
                    {
                        // Register all consumers
                        cfg.AddConsumer<ProductCreatedConsumer>();
                        cfg.AddConsumer<OrderCreatedConsumer>();
                        cfg.AddConsumer<AccessControlConsumer>();
                        cfg.AddConsumer<DeviceStatusChangedConsumer>();
                        cfg.AddConsumer<AlarmTriggeredConsumer>();
                        cfg.AddConsumer<PersonSyncedConsumer>();
                        cfg.AddConsumer<DoorOpenedConsumer>();
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}

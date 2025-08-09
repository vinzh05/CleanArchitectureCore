
using Ecom.Infrastructure.DI;
using Ecom.Infrastructure.Messaging;
using Infrastructure.Consumers.Product;
using MassTransit;

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

                    services.AddInfrastructure(config, addOutboxPublisher: true);

                    // Register consumers in worker assembly
                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<ProductCreatedConsumer>();
                    });

                    // Configure MassTransit consumer endpoints based on config and map consumers per exchange key
                    MassTransitConfig.AddMassTransitConsumers(services, config, (context, endpoint, key) =>
                    {
                        if (key == "ProductCreated")
                            endpoint.ConfigureConsumer<ProductCreatedConsumer>(context);
                        // add other mapping rules as needed
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}

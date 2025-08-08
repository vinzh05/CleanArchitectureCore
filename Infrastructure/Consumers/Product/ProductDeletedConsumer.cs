using Infrastructure.Consumers.Common;
using Infrastructure.IntegrationEvents.Product;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Consumers.Product
{
    public class ProductDeletedConsumer : TConsumer<ProductDeletedIntegrationEvent>
    {
        public override Task Consume(ConsumeContext<ProductDeletedIntegrationEvent> context)
        {
            Console.WriteLine($"[ProductDeletedConsumer] Product deleted with data: {context.Message}");
            return base.Consume(context);
        }
    }
}

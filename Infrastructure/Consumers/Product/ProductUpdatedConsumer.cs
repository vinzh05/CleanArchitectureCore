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
    public class ProductUpdatedConsumer : TConsumer<ProductUpdatedIntegrationEvent>
    {
        public override Task Consume(ConsumeContext<ProductUpdatedIntegrationEvent> context)
        {
            Console.WriteLine($"[ProductUpdatedConsumer] Product updated with data: {context.Message}");
            return base.Consume(context);
        }
    }
}

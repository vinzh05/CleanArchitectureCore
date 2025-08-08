using Infrastructure.Consumers.Common;
using Infrastructure.Contracts.Product;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Consumers.Product
{
    public class ProductCreatedConsumer : TConsumer<ProductCreatedIntegrationEvent>
    {
        public override Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
        {
            Console.WriteLine($"[ProductCreatedConsumer] Product created with data: {context.Message}");
            return base.Consume(context);
        }
    }
}

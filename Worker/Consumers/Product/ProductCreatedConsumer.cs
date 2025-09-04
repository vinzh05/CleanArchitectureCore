using Infrastructure.Consumers.Common;
using Infrastructure.Search;
using MassTransit;
using Shared.Contracts.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Consumers.Product
{
    public class ProductCreatedConsumer : IConsumer<ProductCreatedIntegrationEvent>
    {
        private readonly ElasticService _elastic;
        private readonly ILogger<ProductCreatedConsumer> _logger;

        public ProductCreatedConsumer(ElasticService elastic, ILogger<ProductCreatedConsumer> logger)
        {
            _elastic = elastic; _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Index product {Id}", msg.ProductId);
            await _elastic.IndexAsync(new { id = msg.ProductId, name = msg.Name, price = msg.Price });
        }
    }
}

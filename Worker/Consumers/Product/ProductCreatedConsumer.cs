using Infrastructure.Consumers.Common;
using Infrastructure.Search;
using MassTransit;
using Shared.IntegrationEvents.Contracts.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Service; // Thêm để inject IProductService
using Application.Contracts.Product; // Thêm cho ProductRequest
using Shared.Common; // Thêm cho Result

namespace Infrastructure.Consumers.Product
{
    /// <summary>
    /// Consumer cho ProductCreatedIntegrationEvent từ RabbitMQ qua MassTransit.
    /// Xử lý event khi sản phẩm được tạo, kiểm tra và đồng bộ dữ liệu với database/ElasticSearch.
    /// Kế thừa IConsumer để nhận và consume message từ queue.
    /// </summary>
    public class ProductCreatedConsumer : IConsumer<ProductCreatedIntegrationEvent>
    {
        private readonly ElasticService _elastic;
        private readonly ILogger<ProductCreatedConsumer> _logger;
        private readonly IProductService _productService; // Inject ProductService

        /// <summary>
        /// Khởi tạo ProductCreatedConsumer với các dependency: ElasticService, Logger và ProductService.
        /// </summary>
        public ProductCreatedConsumer(ElasticService elastic, ILogger<ProductCreatedConsumer> logger, IProductService productService)
        {
            _elastic = elastic;
            _logger = logger;
            _productService = productService;
        }

        /// <summary>
        /// Xử lý message ProductCreatedIntegrationEvent từ RabbitMQ.
        /// Kiểm tra sản phẩm tồn tại, cập nhật hoặc tạo mới qua ProductService.
        /// Index dữ liệu vào ElasticSearch và log kết quả.
        /// Xử lý exception và throw lại để MassTransit retry.
        /// </summary>
        public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Processing ProductCreatedIntegrationEvent for Product ID: {Id}", msg.ProductId);

            try
            {
                // Kiểm tra xem Product đã tồn tại chưa
                var existingProductResult = await _productService.GetProductByIdAsync(msg.ProductId);
                if (existingProductResult.Success && existingProductResult.Data != null)
                {
                    // Nếu tồn tại, cập nhật thông tin (ví dụ: sync price nếu cần)
                    var updateRequest = new ProductRequest
                    {
                        Name = msg.Name,
                        Description = existingProductResult.Data.Description, // Giữ nguyên description
                        Price = msg.Price,
                        Stock = existingProductResult.Data.Stock // Giữ nguyên stock
                    };
                    var updateResult = await _productService.UpdateProductAsync(msg.ProductId, updateRequest);
                    if (updateResult.Success)
                    {
                        _logger.LogInformation("Updated existing Product: {Name}", msg.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update Product: {Error}", updateResult.Message);
                    }
                }
                else
                {
                    // Nếu chưa tồn tại, tạo mới (giả sử stock mặc định = 0, description mặc định)
                    var createRequest = new ProductRequest
                    {
                        Name = msg.Name,
                        Description = "Created from integration event", // Mặc định
                        Price = msg.Price,
                        Stock = 0 // Mặc định
                    };
                    var createResult = await _productService.CreateProductAsync(createRequest);
                    if (createResult.Success)
                    {
                        _logger.LogInformation("Created new Product: {Name}", msg.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create Product: {Error}", createResult.Message);
                    }
                }

                // Vẫn index vào Elastic như cũ
                await _elastic.IndexAsync(new { id = msg.ProductId, name = msg.Name, price = msg.Price });
                _logger.LogInformation("Indexed product {Id}", msg.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ProductCreatedIntegrationEvent for Product ID: {Id}", msg.ProductId);
                throw;
            }
        }
    }
}

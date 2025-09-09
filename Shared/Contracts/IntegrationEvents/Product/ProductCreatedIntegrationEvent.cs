using System;

namespace Shared.IntegrationEvents.Contracts.Product
{
    /// <summary>
    /// Sự kiện tích hợp được publish khi sản phẩm được tạo.
    /// Chứa thông tin cơ bản của sản phẩm để đồng bộ với các hệ thống khác qua RabbitMQ.
    /// </summary>
    public record ProductCreatedIntegrationEvent(Guid ProductId, string Name, decimal Price);
}

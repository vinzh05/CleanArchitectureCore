using Shared.Common;
using System;
using System.Collections.Generic;

namespace Shared.IntegrationEvents.Contracts.Order
{
    /// <summary>
    /// Sự kiện tích hợp được publish khi đơn hàng được tạo.
    /// Chứa thông tin đơn hàng để đồng bộ với các hệ thống khác qua RabbitMQ.
    /// </summary>
    public class OrderCreatedIntegrationEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public List<OrderItemIntegration> Items { get; set; } = new();
    }

    public class OrderItemIntegration
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}

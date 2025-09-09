using Domain.Common;
using Domain.Items;
using MediatR;
using System;
using System.Collections.Generic;

namespace Domain.Events.Order
{
    /// <summary>
    /// Sự kiện domain được raise khi đơn hàng được tạo trong hệ thống.
    /// Kế thừa BaseEvent và INotification để xử lý qua MediatR.
    /// Chứa thông tin đơn hàng để các handler xử lý (ví dụ: gửi notification hoặc publish integration event).
    /// </summary>
    public class OrderCreatedDomainEvent : BaseEvent, INotification
    {
        /// <summary>
        /// ID của đơn hàng.
        /// </summary>
        public Guid OrderId { get; }

        /// <summary>
        /// Số đơn hàng.
        /// </summary>
        public string OrderNumber { get; }

        /// <summary>
        /// Tổng giá trị đơn hàng.
        /// </summary>
        public decimal Total { get; }

        /// <summary>
        /// Danh sách các item trong đơn hàng.
        /// </summary>
        public List<OrderItem> Items { get; }

        /// <summary>
        /// Khởi tạo sự kiện với thông tin đơn hàng.
        /// </summary>
        /// <param name="orderId">ID đơn hàng.</param>
        /// <param name="orderNumber">Số đơn hàng.</param>
        /// <param name="total">Tổng giá.</param>
        /// <param name="items">Danh sách items.</param>
        public OrderCreatedDomainEvent(Guid orderId, string orderNumber, decimal total, List<OrderItem> items)
        {
            OrderId = orderId;
            OrderNumber = orderNumber;
            Total = total;
            Items = items ?? new List<OrderItem>();
        }
    }
}

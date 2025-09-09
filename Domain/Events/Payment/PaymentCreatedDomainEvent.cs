using Domain.Common;
using MediatR;
using System;

namespace Domain.Events.Payment
{
    /// <summary>
    /// Sự kiện domain được raise khi thanh toán được tạo.
    /// </summary>
    public class PaymentCreatedDomainEvent : BaseEvent, INotification
    {
        public Guid PaymentId { get; }
        public Guid OrderId { get; }
        public decimal Amount { get; }
        public string PaymentMethod { get; }
        public DateTime ExpiryDate { get; }

        public PaymentCreatedDomainEvent(Guid paymentId, Guid orderId, decimal amount, string paymentMethod, DateTime expiryDate)
        {
            PaymentId = paymentId;
            OrderId = orderId;
            Amount = amount;
            PaymentMethod = paymentMethod;
            ExpiryDate = expiryDate;
        }
    }
}

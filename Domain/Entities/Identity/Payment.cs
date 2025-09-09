using Domain.Common;
using Domain.Enums;
using Domain.Events.Payment;
using System;

namespace Domain.Entities.Identity
{
    /// <summary>
    /// Entity đại diện cho thanh toán.
    /// </summary>
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; private set; }
        public decimal Amount { get; private set; }
        public string PaymentMethod { get; private set; } = string.Empty;
        public PaymentStatus Status { get; private set; }
        public DateTime ExpiryDate { get; private set; }

        private Payment() { }

        public Payment(Guid orderId, decimal amount, string paymentMethod)
        {
            OrderId = orderId;
            Amount = amount;
            PaymentMethod = paymentMethod;
            Status = PaymentStatus.Pending;
            ExpiryDate = DateTime.UtcNow.AddMinutes(5);

            // Raise domain event
            AddDomainEvent(new PaymentCreatedDomainEvent(Id, orderId, amount, paymentMethod, ExpiryDate));
        }

        public void CompletePayment()
        {
            if (Status == PaymentStatus.Pending && DateTime.UtcNow <= ExpiryDate)
                Status = PaymentStatus.Completed;
        }

        public void FailPayment()
        {
            Status = PaymentStatus.Failed;
        }

        public bool IsExpired() => DateTime.UtcNow > ExpiryDate && Status == PaymentStatus.Pending;
    }
}

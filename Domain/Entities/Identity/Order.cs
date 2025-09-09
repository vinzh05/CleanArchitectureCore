using Domain.Common;
using Domain.Events.Order;
using Domain.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Identity
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; private set; } = string.Empty;
        public decimal Total { get; private set; }
        public List<OrderItem> Items { get; private set; } = new();

        private Order() { }

        public Order(string orderNumber, List<OrderItem> items)
        {
            OrderNumber = orderNumber;
            Items = items ?? new List<OrderItem>();
            Total = Items.Sum(i => i.GetTotalPrice());

            // Raise domain event
            AddDomainEvent(new OrderCreatedDomainEvent(Id, orderNumber, Total, Items));
        }
    }

}

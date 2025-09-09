using System.Collections.Generic;

namespace Application.Contracts.Order
{
    public class OrderRequest
    {
        public Guid CustomerId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

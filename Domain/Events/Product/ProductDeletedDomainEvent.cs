using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Events.Product
{
    public class ProductDeletedDomainEvent : BaseEvent
    {
        public Guid ProductId { get; }

        public ProductDeletedDomainEvent(Guid productId)
        {
            ProductId = productId;
        }
    }
}

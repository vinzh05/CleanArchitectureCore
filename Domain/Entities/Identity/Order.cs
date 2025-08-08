using Domain.Common;
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

        private Order() { }

        public Order(string orderNumber, decimal total)
        {
            OrderNumber = orderNumber;
            Total = total;
        }
    }

}

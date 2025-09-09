using Domain.Entities.Identity;
using Domain.Items;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Service
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string orderNumber, List<OrderItem> items);
        Task<Order?> GetByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetAllAsync();
    }
}

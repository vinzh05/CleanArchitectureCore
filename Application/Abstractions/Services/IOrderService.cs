using Domain.Entities.Identity;
using Domain.Items;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Service
{
    public interface IOrderService
    {
        Task<Result<Order>> CreateOrderAsync(string orderNumber, List<OrderItem> items);
        Task<Result<Order>> GetByIdAsync(Guid id);
        Task<Result<IEnumerable<Order>>> GetAllAsync();
    }
}

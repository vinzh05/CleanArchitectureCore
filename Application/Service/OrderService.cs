using Application.Abstractions;
using Application.Abstractions.Common;
using Application.Abstractions.Service;
using Domain.Entities.Identity;
using Domain.Items;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Order> CreateOrderAsync(string orderNumber, List<OrderItem> items)
        {
            await _unitOfWork.BeginTransactionAsync();

            var order = new Order(orderNumber, items);
            await _unitOfWork.Orders.AddAsync(order);

            var ok = await _unitOfWork.CommitTransactionAsync();
            if (!ok) throw new Exception("Commit failed");

            return order;
        }

        public async Task<IEnumerable<Order>> GetAllAsync() => await _unitOfWork.Orders.GetAllAsync();

        public async Task<Order?> GetByIdAsync(Guid id) => await _unitOfWork.Orders.GetByIdAsync(id);
    }
}

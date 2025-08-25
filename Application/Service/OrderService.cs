using Application.Abstractions;
using Application.Abstractions.Common;
using Application.Abstractions.Service;
using Domain.Entities;
using Domain.Entities.Identity;
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
            _unitOfWork = unitOfWork;
        }

        public async Task<Order> CreateOrderAsync(string orderNumber, decimal total)
        {
            await _unitOfWork.BeginTransactionAsync();

            var order = new Order(orderNumber, total);
            await _unitOfWork.Orders.AddAsync(order);

            var ok = await _unitOfWork.CommitTransactionAsync();
            if (!ok) throw new Exception("Commit failed");

            return order;
        }

        public async Task<IEnumerable<Order>> GetAllAsync() => await _unitOfWork.Orders.GetAllAsync();

        public async Task<Order?> GetByIdAsync(Guid id) => await _unitOfWork.Orders.GetByIdAsync(id);
    }
}

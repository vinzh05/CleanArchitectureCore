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

        public async Task<Result<Order>> CreateOrderAsync(string orderNumber, List<OrderItem> items)
        {
            await _unitOfWork.BeginTransactionAsync();

            var order = new Order(orderNumber, items);
            await _unitOfWork.Orders.AddAsync(order);

            var ok = await _unitOfWork.CommitTransactionAsync();
            if (!ok) throw new Exception("Commit failed");

            return Result<Order>.SuccessResult(order, "Tạo đơn hàng thành công", HttpStatusCode.Created);
        }

        public async Task<Result<IEnumerable<Order>>> GetAllAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            return Result<IEnumerable<Order>>.SuccessResult(orders, "Lấy danh sách đơn hàng thành công");
        }

        public async Task<Result<Order>> GetByIdAsync(Guid id)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null)
                return Result<Order>.FailureResult("Không tìm thấy đơn hàng", "ORDER_NOT_FOUND", HttpStatusCode.NotFound);
            return Result<Order>.SuccessResult(order, "Lấy đơn hàng thành công");
        }
    }
}

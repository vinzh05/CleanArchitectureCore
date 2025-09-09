using Domain.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Events.Product
{
    /// <summary>
    /// Sự kiện domain được raise khi sản phẩm được tạo trong hệ thống.
    /// Kế thừa BaseEvent và INotification để xử lý qua MediatR.
    /// Chứa thông tin sản phẩm để các handler xử lý (ví dụ: gửi notification hoặc publish integration event).
    /// </summary>
    public class ProductCreatedDomainEvent : BaseEvent, INotification
    {
        public Guid ProductId { get; }
        public string Name { get; }
        public decimal Price { get; }

        /// <summary>
        /// Khởi tạo sự kiện với thông tin sản phẩm.
        /// </summary>
        /// <param name="productId">ID của sản phẩm.</param>
        /// <param name="name">Tên sản phẩm.</param>
        /// <param name="price">Giá sản phẩm.</param>
        public ProductCreatedDomainEvent(Guid productId, string name, decimal price)
        {
            ProductId = productId;
            Name = name;
            Price = price;
        }
    }
}

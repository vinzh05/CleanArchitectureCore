using System;

namespace Domain.Items
{
    /// <summary>
    /// Value Object cho item trong đơn hàng.
    /// Đại diện cho một sản phẩm cụ thể với số lượng và giá trong đơn hàng.
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// ID của sản phẩm.
        /// </summary>
        public Guid ProductId { get; }

        /// <summary>
        /// Số lượng sản phẩm.
        /// </summary>
        public int Quantity { get; }

        /// <summary>
        /// Giá của sản phẩm tại thời điểm đặt.
        /// </summary>
        public decimal Price { get; }

        /// <summary>
        /// Khởi tạo OrderItem với thông tin sản phẩm.
        /// </summary>
        /// <param name="productId">ID sản phẩm.</param>
        /// <param name="quantity">Số lượng.</param>
        /// <param name="price">Giá.</param>
        public OrderItem(Guid productId, int quantity, decimal price)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
            if (price < 0) throw new ArgumentException("Price cannot be negative", nameof(price));

            ProductId = productId;
            Quantity = quantity;
            Price = price;
        }

        /// <summary>
        /// Tính tổng giá cho item này (Quantity * Price).
        /// </summary>
        public decimal GetTotalPrice() => Quantity * Price;
    }
}

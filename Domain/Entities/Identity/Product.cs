using Domain.Common;
using Domain.Events;
using Domain.Events.Product;
using System;

namespace Domain.Entities.Identity
{
    public class Product : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int Stock { get; private set; }

        private Product() { } // EF Core

        public Product(string name, string description, decimal price, int stock)
        {
            ValidateAndSetProperties(name, description, price, stock);
            AddDomainEvent(new ProductCreatedDomainEvent(Id, name, price));
        }

        public void UpdateInfo(string name, string description, decimal price)
        {
            ValidateAndSetProperties(name, description, price, Stock);
        }

        public void UpdateStock(int stock)
        {
            if (stock < 0) throw new InvalidOperationException("Stock cannot be negative.");
            Stock = stock;
        }

        private void ValidateAndSetProperties(string name, string description, decimal price, int stock)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            if (price < 0)
                throw new ArgumentException("Price cannot be negative.", nameof(price));
            if (stock < 0)
                throw new ArgumentException("Stock cannot be negative.", nameof(stock));

            Name = name;
            Description = description ?? string.Empty;
            Price = price;
            Stock = stock;
        }
    }
}
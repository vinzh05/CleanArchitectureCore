using Application.DTOs.Product;
using Domain.Entities.Identity;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Common.Interface
{
    public interface IProductService
    {
        Task<Result<Product>> CreateProductAsync(ProductRequest request);
        Task<Result<Product>> GetProductByIdAsync(Guid id);
        Task<Result<Product>> UpdateProductAsync(Guid id, ProductRequest request);
        Task<Result<bool>> DeleteProductAsync(Guid id);
        Task<Result<List<Product>>> SearchProductsAsync(string query, int from = 0, int size = 20);
    }
}
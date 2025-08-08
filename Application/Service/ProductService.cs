using Application.Abstractions.Common;
using Application.Common.Interface;
using Application.DTOs.Product;
using Domain.Entities.Identity;
using Shared.Common;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Application.Service
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRedisCacheService _cache;
        private readonly IElasticService _elasticService;

        public ProductService(IUnitOfWork unitOfWork, IRedisCacheService cache, IElasticService elasticService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _elasticService = elasticService ?? throw new ArgumentNullException(nameof(elasticService));
        }

        public async Task<Result<Product>> CreateProductAsync(ProductRequest request)
        {
            try
            {
                var product = new Product(request.Name, request.Description, request.Price, request.Stock);
                await _unitOfWork.Products.AddAsync(product);

                var success = await _unitOfWork.CommitTransactionAsync();
                if (!success)
                {
                    return Result<Product>.FailureResult("Tạo sản phẩm thất bại.", statusCode: HttpStatusCode.InternalServerError);
                }

                await _elasticService.IndexAsync(product);
                var cacheKey = GetCacheKey(product.Id);
                await _cache.SetAsync(cacheKey, product, TimeSpan.FromMinutes(10));

                return Result<Product>.SuccessResult(product, statusCode: HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                return Result<Product>.FailureResult($"Tạo sản phẩm thất bại: {ex.Message}", statusCode: HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Result<Product>> GetProductByIdAsync(Guid id)
        {
            try
            {
                var cacheKey = GetCacheKey(id);
                var cachedProduct = await _cache.GetAsync<Product>(cacheKey);
                if (cachedProduct != null)
                {
                    return Result<Product>.SuccessResult(cachedProduct);
                }

                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                {
                    return Result<Product>.FailureResult("Không tìm thấy sản phẩm.", statusCode: HttpStatusCode.NotFound);
                }

                await _cache.SetAsync(cacheKey, product, TimeSpan.FromMinutes(10));
                return Result<Product>.SuccessResult(product);
            }
            catch (Exception ex)
            {
                return Result<Product>.FailureResult($"Lấy sản phẩm thất bại: {ex.Message}", statusCode: HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Result<Product>> UpdateProductAsync(Guid id, ProductRequest request)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                {
                    return Result<Product>.FailureResult("Không tìm thấy sản phẩm cần cập nhật.", statusCode: HttpStatusCode.NotFound);
                }

                product.UpdateStock(request.Stock);
                product.UpdateInfo(request.Name, request.Description, request.Price);

                _unitOfWork.Products.Update(product);
                var success = await _unitOfWork.CommitTransactionAsync();
                if (!success)
                {
                    return Result<Product>.FailureResult("Cập nhật sản phẩm thất bại.", statusCode: HttpStatusCode.InternalServerError);
                }

                await _elasticService.IndexAsync(product);
                var cacheKey = GetCacheKey(id);
                await _cache.RemoveAsync(cacheKey);

                return Result<Product>.SuccessResult(product);
            }
            catch (Exception ex)
            {
                return Result<Product>.FailureResult($"Cập nhật sản phẩm thất bại: {ex.Message}", statusCode: HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Result<bool>> DeleteProductAsync(Guid id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                {
                    return Result<bool>.FailureResult("Không tìm thấy sản phẩm cần xóa.", statusCode: HttpStatusCode.NotFound);
                }

                product.MarkAsDeleted();
                _unitOfWork.Products.Remove(product);
                var success = await _unitOfWork.CommitTransactionAsync();
                if (!success)
                {
                    return Result<bool>.FailureResult("Xóa sản phẩm thất bại.", statusCode: HttpStatusCode.InternalServerError);
                }

                await _elasticService.DeleteAsync<Product>(id.ToString(), "products");
                var cacheKey = GetCacheKey(id);
                await _cache.RemoveAsync(cacheKey);

                return Result<bool>.SuccessResult(true, statusCode: HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return Result<bool>.FailureResult($"Xóa sản phẩm thất bại: {ex.Message}", statusCode: HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Result<List<Product>>> SearchProductsAsync(string query, int from = 0, int size = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Result<List<Product>>.FailureResult("Query không được để trống.", statusCode: HttpStatusCode.BadRequest);
                }

                var response = await _elasticService.SearchAsync(query, from, size);
                if (!response.IsValid || !response.Documents.Any())
                {
                    return Result<List<Product>>.FailureResult("Không tìm thấy sản phẩm nào.", statusCode: HttpStatusCode.NotFound);
                }

                var products = response.Documents.ToList();
                return Result<List<Product>>.SuccessResult(products);
            }
            catch (Exception ex)
            {
                return Result<List<Product>>.FailureResult($"Tìm kiếm sản phẩm thất bại: {ex.Message}", statusCode: HttpStatusCode.InternalServerError);
            }
        }

        private string GetCacheKey(Guid id) => $"product:{id}";
    }
}
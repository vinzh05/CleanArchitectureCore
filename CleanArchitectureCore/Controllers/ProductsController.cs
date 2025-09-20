using Application.Abstractions.Common;
using Application.Abstractions.Service;
using Application.Contracts.Product;
using ChatDakenh.Controllers.Common;
using Domain.Entities.Identity;
using Infrastructure.Cache;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitectureCore.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : BaseController
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) => await HandleAsync(_productService.GetProductByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductRequest req) => await HandleAsync(_productService.CreateProductAsync(req));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductRequest req) => await HandleAsync(_productService.UpdateProductAsync(id, req));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id) => await HandleAsync(_productService.DeleteProductAsync(id));
    }
}

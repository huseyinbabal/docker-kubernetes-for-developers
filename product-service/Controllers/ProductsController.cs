using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromBody] ProductCreateDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdProduct = await _productService.CreateProductAsync(productDto);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(long id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            return product != null ? Ok(product) : NotFound();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProductsByCategory(string category)
        {
            var products = await _productService.GetProductsByCategoryAsync(category);
            return Ok(products);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductResponseDto>> UpdateProduct(long id, [FromBody] ProductCreateDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedProduct = await _productService.UpdateProductAsync(id, productDto);
            return updatedProduct != null ? Ok(updatedProduct) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var deleted = await _productService.DeleteProductAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(long id, [FromBody] StockUpdateDto stockUpdate)
        {
            var updated = await _productService.UpdateStockAsync(id, stockUpdate.Quantity);
            return updated ? Ok() : NotFound();
        }

        [HttpGet("stats")]
        public async Task<ActionResult<Dictionary<string, object>>> GetProductStats()
        {
            var stats = await _productService.GetProductStatsAsync();
            return Ok(stats);
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "UP",
                Service = "product-service",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
    }

    public class StockUpdateDto
    {
        public int Quantity { get; set; }
    }
}
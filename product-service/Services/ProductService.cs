using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Services.Messaging;

namespace ProductService.Services
{
    public class ProductServiceImpl : IProductService
    {
        private readonly ProductDbContext _context;
        private readonly IMessagePublisher _messagePublisher;

        public ProductServiceImpl(ProductDbContext context, IMessagePublisher messagePublisher)
        {
            _context = context;
            _messagePublisher = messagePublisher;
        }

        public async Task<ProductResponseDto> CreateProductAsync(ProductCreateDto productDto)
        {
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                StockQuantity = productDto.StockQuantity,
                Category = productDto.Category,
                ImageUrl = productDto.ImageUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _messagePublisher.PublishProductCreatedAsync(product);

            return MapToDto(product);
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(long id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            return product != null ? MapToDto(product) : null;
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return products.Select(MapToDto);
        }

        public async Task<IEnumerable<ProductResponseDto>> GetProductsByCategoryAsync(string category)
        {
            var products = await _context.Products
                .Where(p => p.IsActive && p.Category.ToLower() == category.ToLower())
                .OrderBy(p => p.Name)
                .ToListAsync();

            return products.Select(MapToDto);
        }

        public async Task<ProductResponseDto?> UpdateProductAsync(long id, ProductCreateDto productDto)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return null;

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.StockQuantity = productDto.StockQuantity;
            product.Category = productDto.Category;
            product.ImageUrl = productDto.ImageUrl;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(product);
        }

        public async Task<bool> DeleteProductAsync(long id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return false;

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockAsync(long id, int quantity)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return false;

            product.StockQuantity = Math.Max(0, product.StockQuantity - quantity);
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (product.StockQuantity == 0)
            {
                await _messagePublisher.PublishProductOutOfStockAsync(product);
            }

            return true;
        }

        public async Task<Dictionary<string, object>> GetProductStatsAsync()
        {
            var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
            var totalValue = await _context.Products
                .Where(p => p.IsActive)
                .SumAsync(p => p.Price * p.StockQuantity);
            var categories = await _context.Products
                .Where(p => p.IsActive)
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            return new Dictionary<string, object>
            {
                { "totalProducts", totalProducts },
                { "totalInventoryValue", totalValue },
                { "categories", categories },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
            };
        }

        private static ProductResponseDto MapToDto(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                Category = product.Category,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}
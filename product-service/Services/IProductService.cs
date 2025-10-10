using ProductService.DTOs;

namespace ProductService.Services
{
    public interface IProductService
    {
        Task<ProductResponseDto> CreateProductAsync(ProductCreateDto productDto);
        Task<ProductResponseDto?> GetProductByIdAsync(long id);
        Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync();
        Task<IEnumerable<ProductResponseDto>> GetProductsByCategoryAsync(string category);
        Task<ProductResponseDto?> UpdateProductAsync(long id, ProductCreateDto productDto);
        Task<bool> DeleteProductAsync(long id);
        Task<bool> UpdateStockAsync(long id, int quantity);
        Task<Dictionary<string, object>> GetProductStatsAsync();
    }
}
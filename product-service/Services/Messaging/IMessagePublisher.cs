using ProductService.Models;

namespace ProductService.Services.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishProductCreatedAsync(Product product);
        Task PublishProductOutOfStockAsync(Product product);
        Task PublishStockUpdatedAsync(Product product, int quantityChanged);
    }
}
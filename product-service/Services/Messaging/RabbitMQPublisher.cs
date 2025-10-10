using Newtonsoft.Json;
using ProductService.Models;
using RabbitMQ.Client;
using System.Text;

namespace ProductService.Services.Messaging
{
    public class RabbitMQPublisher : IMessagePublisher
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string PRODUCT_EXCHANGE = "product.exchange";

        public RabbitMQPublisher(IConfiguration configuration)
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost",
                Port = configuration.GetValue<int>("RabbitMQ:Port", 5672),
                UserName = configuration.GetValue<string>("RabbitMQ:Username") ?? "guest",
                Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "guest",
                VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(PRODUCT_EXCHANGE, ExchangeType.Topic, durable: true);
        }

        public async Task PublishProductCreatedAsync(Product product)
        {
            var message = new
            {
                EventType = "PRODUCT_CREATED",
                ProductId = product.Id,
                Name = product.Name,
                Category = product.Category,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await PublishMessageAsync("product.created", message);
        }

        public async Task PublishProductOutOfStockAsync(Product product)
        {
            var message = new
            {
                EventType = "PRODUCT_OUT_OF_STOCK",
                ProductId = product.Id,
                Name = product.Name,
                Category = product.Category,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await PublishMessageAsync("product.outofstock", message);
        }

        public async Task PublishStockUpdatedAsync(Product product, int quantityChanged)
        {
            var message = new
            {
                EventType = "STOCK_UPDATED",
                ProductId = product.Id,
                Name = product.Name,
                NewStockQuantity = product.StockQuantity,
                QuantityChanged = quantityChanged,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await PublishMessageAsync("product.stock.updated", message);
        }

        private async Task PublishMessageAsync(string routingKey, object message)
        {
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            await Task.Run(() => _channel.BasicPublish(
                exchange: PRODUCT_EXCHANGE,
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            ));
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
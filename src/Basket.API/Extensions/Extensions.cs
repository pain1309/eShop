using System.Text.Json.Serialization;
using eShop.Basket.API.Repositories;
using eShop.Basket.API.IntegrationEvents.EventHandling;
using eShop.Basket.API.IntegrationEvents.EventHandling.Events;

namespace eShop.Basket.API.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultAuthentication();

        // Add Redis client with connection name "redis"
        // Redis is used as a distributed cache to store shopping basket data
        // This registers IConnectionMultiplexer and IDistributedCache services
        builder.AddRedisClient("redis");

        builder.Services.AddSingleton<IBasketRepository, RedisBasketRepository>();

        // Add RabbitMQ event bus with connection name "eventbus"
        // This registers event bus services and configures RabbitMQ connection
        builder.AddRabbitMqEventBus("eventbus")
               // Subscribe to OrderStartedIntegrationEvent with OrderStartedIntegrationEventHandler
               // This handler will process order started events from the event bus
               .AddSubscription<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>()
               // Configure JSON serialization options to include the IntegrationEventContext
               // This enables proper serialization/deserialization of integration events
               .ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventContext.Default));
    }
}

// Đoạn code này có mục đích:
// 1. Tạo một lớp để hỗ trợ serialize/deserialize JSON cho các integration event
// 2. Attribute [JsonSerializable] cho biết OrderStartedIntegrationEvent cần được tạo code serialize tự động
// 3. Kế thừa từ JsonSerializerContext để có metadata serialization lúc compile-time
// 4. Dùng partial class để cho phép source generator tự động tạo implementation
// 5. Được sử dụng trong phần cấu hình event bus để xử lý các event message
[JsonSerializable(typeof(OrderStartedIntegrationEvent))]
partial class IntegrationEventContext : JsonSerializerContext
{

}

using eShop.Basket.API.Repositories;
using eShop.Basket.API.IntegrationEvents.EventHandling.Events;

namespace eShop.Basket.API.IntegrationEvents.EventHandling;

// Đây là handler xử lý sự kiện OrderStartedIntegrationEvent
// Sử dụng primary constructor trong C# 12 để inject các dependency:
// - IBasketRepository: để thao tác với basket data
// - ILogger: để ghi log
public class OrderStartedIntegrationEventHandler(
    IBasketRepository repository,    // Repository để thao tác với basket
    ILogger<OrderStartedIntegrationEventHandler> logger) // Logger để ghi log
    : IIntegrationEventHandler<OrderStartedIntegrationEvent> // Implement interface để xử lý event
{
    // Phương thức Handle được gọi khi nhận được event OrderStartedIntegrationEvent
    public async Task Handle(OrderStartedIntegrationEvent @event)
    {
        // Ghi log thông tin về event đang được xử lý
        // Sử dụng structured logging với các placeholder {} 
        // để log ID và nội dung của event
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        // Xóa basket của user sau khi order đã được bắt đầu
        // vì basket không còn cần thiết nữa
        await repository.DeleteBasketAsync(@event.UserId);
    }
}

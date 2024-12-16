namespace eShop.Catalog.API.IntegrationEvents;

public sealed class CatalogIntegrationEventService(ILogger<CatalogIntegrationEventService> logger,
    IEventBus eventBus,
    CatalogContext catalogContext,
    IIntegrationEventLogService integrationEventLogService)
    : ICatalogIntegrationEventService, IDisposable
{
    private volatile bool disposedValue;

    public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
    {
        try
        {
            logger.LogInformation("Publishing integration event: {IntegrationEventId_published} - ({@IntegrationEvent})", evt.Id, evt);

            await integrationEventLogService.MarkEventAsInProgressAsync(evt.Id);
            await eventBus.PublishAsync(evt);
            await integrationEventLogService.MarkEventAsPublishedAsync(evt.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", evt.Id, evt);
            await integrationEventLogService.MarkEventAsFailedAsync(evt.Id);
        }
    }

    public async Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt)
    {
        logger.LogInformation("CatalogIntegrationEventService - Saving changes and integrationEvent: {IntegrationEventId}", evt.Id);

        //Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
        //See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency            
        await ResilientTransaction.New(catalogContext).ExecuteAsync(async () =>
        {
            // Sử dụng ResilientTransaction để thực hiện các thao tác database một cách đáng tin cậy
            // bằng cách tự động retry khi có lỗi tạm thời xảy ra
            
            // Thực hiện 2 thao tác trong cùng một transaction để đảm bảo tính nguyên tử:
            // 1. Lưu các thay đổi trong catalogContext vào database
            await catalogContext.SaveChangesAsync();
            
            // 2. Lưu event vào bảng IntegrationEventLog để:
            // - Đảm bảo event được lưu lại để có thể retry khi gửi thất bại
            // - Tránh mất event khi hệ thống gặp sự cố
            // - Sử dụng chung transaction với catalogContext.SaveChanges() 
            //   để đảm bảo tính nhất quán của dữ liệu:
            //   + Nếu lưu event thất bại -> rollback luôn việc SaveChanges
            //   + Nếu SaveChanges thất bại -> không lưu event
            await integrationEventLogService.SaveEventAsync(evt, catalogContext.Database.CurrentTransaction);
        });
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                (integrationEventLogService as IDisposable)?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

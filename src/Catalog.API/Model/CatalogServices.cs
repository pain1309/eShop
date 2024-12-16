using eShop.Catalog.API.Services;

// CatalogServices là một class chứa các dependency cần thiết cho Catalog API
// Sử dụng primary constructor trong C# 12 để inject các dependency:
// - context: CatalogContext để truy cập database
// - catalogAI: ICatalogAI để tích hợp AI 
// - options: IOptions<CatalogOptions> để cấu hình
// - logger: ILogger để ghi log
// - eventService: ICatalogIntegrationEventService để xử lý integration events
public class CatalogServices(
    CatalogContext context,
    ICatalogAI catalogAI, 
    IOptions<CatalogOptions> options,
    ILogger<CatalogServices> logger,
    ICatalogIntegrationEventService eventService)
{
    // Các property chỉ đọc để truy cập các dependency đã inject
    // Đây là cách viết property initialization với read-only auto-property
    // - { get; } = value; khai báo một property chỉ đọc và khởi tạo giá trị ngay lập tức
    // - Giá trị chỉ được gán 1 lần khi khởi tạo và không thể thay đổi sau đó
    // - Tương đương với việc gán giá trị trong constructor
    public CatalogContext Context { get; } = context;
    public ICatalogAI CatalogAI { get; } = catalogAI;
    public IOptions<CatalogOptions> Options { get; } = options;
    public ILogger<CatalogServices> Logger { get; } = logger;
    public ICatalogIntegrationEventService EventService { get; } = eventService;
}

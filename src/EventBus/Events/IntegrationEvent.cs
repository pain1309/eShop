namespace eShop.EventBus.Events;

public record IntegrationEvent
{
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.UtcNow;
    }

    [JsonInclude]
    public Guid Id { get; set; }

    // Nếu không có [JsonInclude], các thuộc tính này có thể không được bao gồm trong JSON nếu chúng không có giá trị (null hoặc giá trị mặc định).
    [JsonInclude]
    public DateTime CreationDate { get; set; }
}

namespace TPGLLC.Data.Entities;

public sealed class ServiceHistoryInspection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceHistoryEntryId { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Condition { get; set; } = "Good";
    public string Finding { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public bool IsCustomerVisible { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }

    public ServiceHistoryEntry? ServiceHistoryEntry { get; set; }
}

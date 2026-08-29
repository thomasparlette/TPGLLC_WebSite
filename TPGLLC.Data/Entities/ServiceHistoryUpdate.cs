namespace TPGLLC.Data.Entities;

public sealed class ServiceHistoryUpdate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceHistoryEntryId { get; set; }
    public string Status { get; set; } = "Update";
    public string Message { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public bool IsCustomerVisible { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public ServiceHistoryEntry? ServiceHistoryEntry { get; set; }
}

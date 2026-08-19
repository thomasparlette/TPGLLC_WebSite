namespace TPGLLC.Data.Entities;

public sealed class ServiceHistoryPart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceHistoryEntryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal? UnitPrice { get; set; }
    public bool IsApplied { get; set; }
    public bool IsApproved { get; set; }
    public bool IsCustomerDeclined { get; set; }
    public ServiceHistoryEntry? ServiceHistoryEntry { get; set; }
}

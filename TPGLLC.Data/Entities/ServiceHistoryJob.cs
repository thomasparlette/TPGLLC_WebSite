namespace TPGLLC.Data.Entities;

public sealed class ServiceHistoryJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceHistoryEntryId { get; set; }
    public Guid? LaborCatalogItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Proposed";
    public decimal? LaborHours { get; set; }
    public decimal? LaborRate { get; set; }
    public decimal? LaborAmount { get; set; }
    public bool IsApproved { get; set; }
    public bool IsCustomerDeclined { get; set; }
    public bool IsDeferred { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }

    public ServiceHistoryEntry? ServiceHistoryEntry { get; set; }
    public LaborCatalogItem? LaborCatalogItem { get; set; }
    public ICollection<ServiceHistoryPart> Parts { get; set; } = [];
}

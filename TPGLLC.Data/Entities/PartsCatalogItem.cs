namespace TPGLLC.Data.Entities;

public sealed class PartsCatalogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }

    public ICollection<ServiceHistoryPart> ServiceHistoryParts { get; set; } = [];
}

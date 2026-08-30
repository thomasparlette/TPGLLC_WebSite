namespace TPGLLC.Data.Entities;

public sealed class LaborCatalogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultHours { get; set; } = 1m;
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }

    public ICollection<ServiceHistoryJob> ServiceHistoryJobs { get; set; } = [];
}

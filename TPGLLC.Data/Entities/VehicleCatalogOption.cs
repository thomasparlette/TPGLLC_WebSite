namespace TPGLLC.Data.Entities;

public sealed class VehicleCatalogOption
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public DateTimeOffset SyncedAtUtc { get; set; }
}

namespace TPGLLC.Shared.Models;

public sealed class VehicleCatalogEntry
{
    public int Id { get; set; }

    public string VehicleType { get; set; } = string.Empty;
    public int ModelYear { get; set; }

    public int MakeId { get; set; }
    public int ModelId { get; set; }

    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public DateTimeOffset SyncedAtUtc { get; set; }
}
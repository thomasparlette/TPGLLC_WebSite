namespace TPGLLC.Web.Infrastructure;

public sealed class VehicleSeedRecord
{
    public int Id { get; set; }              // source id from the JSON
    public int ModelYear { get; set; }
    public int MakeId { get; set; }
    public int ModelId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTimeOffset SyncedAtUtc { get; set; }
}
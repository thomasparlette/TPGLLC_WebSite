namespace TPGLLC.Data.Entities;

public sealed class VehicleCatalogOption
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Source { get; set; } = "vPIC";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset SyncedAtUtc { get; set; }
}

public static class VehicleCatalogOptionCategories
{
    public const string Submodel = "Submodel";
    public const string BodyStyle = "BodyStyle";
    public const string EngineFuel = "EngineFuel";
    public const string Transmission = "Transmission";
    public const string DriveType = "DriveType";
    public const string Brake = "Brake";
}

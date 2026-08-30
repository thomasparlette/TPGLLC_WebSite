namespace TPGLLC.Tools.VehicleImporter;

public sealed class VehicleImportSettings
{
    public int StartYear { get; set; } = 1988;

    public int EndYear { get; set; } = DateTime.UtcNow.Year;

    public int MaxDegreeOfParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);

    public bool ReplaceExisting { get; set; } = false;

    public List<string> AllowedMakes { get; set; } = [];

    public VehicleImportOptionSettings CatalogOptions { get; set; } = new();
}

public sealed class VehicleImportOptionSettings
{
    public List<string> Submodels { get; set; } = [];

    public List<string> BodyStyles { get; set; } = [];

    public List<string> EngineFuelTypes { get; set; } = [];

    public List<string> Transmissions { get; set; } = [];

    public List<string> DriveTypes { get; set; } = [];

    public List<string> Brakes { get; set; } = [];
}

namespace TPGLLC.Tools.VehicleImporter;

public sealed class VehicleImportSettings
{
    public const string SectionName = "VehicleImport";

    public int StartYear { get; set; } = 1996;

    public int EndYear { get; set; } = DateTime.UtcNow.Year;

    public int MaxDegreeOfParallelism { get; set; } = Math.Max(4, Environment.ProcessorCount);

    public string VehicleType { get; set; } = "Automotive";

    public List<string> AllowedMakes { get; set; } = [];
}

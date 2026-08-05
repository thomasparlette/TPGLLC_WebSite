namespace TPGLLC.Tools.VehicleImporter;

public sealed class VehicleImportSettings
{
    public int StartYear { get; set; } = 1996;

    public int EndYear { get; set; } = DateTime.UtcNow.Year;

    public int MaxDegreeOfParallelism { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);

    public bool ReplaceExisting { get; set; } = true;

    public List<string> AllowedMakes { get; set; } = [];
}

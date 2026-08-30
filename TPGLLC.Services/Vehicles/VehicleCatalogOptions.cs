namespace TPGLLC.Services.Vehicles;

public sealed class VehicleCatalogOptions
{
    public IReadOnlyList<string> Submodels { get; init; } = [];
    public IReadOnlyList<string> BodyStyles { get; init; } = [];
    public IReadOnlyList<string> EngineFuelTypes { get; init; } = [];
    public IReadOnlyList<string> Transmissions { get; init; } = [];
    public IReadOnlyList<string> DriveTypes { get; init; } = [];
    public IReadOnlyList<string> Brakes { get; init; } = [];
}

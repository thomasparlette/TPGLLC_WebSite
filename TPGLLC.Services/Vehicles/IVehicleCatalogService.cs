using TPGLLC.Data.Entities;

namespace TPGLLC.Services.Vehicles;

public static class VehicleCatalogOptionCategories
{
    public const string Submodel = "Submodel";
    public const string BodyStyle = "BodyStyle";
    public const string EngineFuel = "EngineFuel";
    public const string Transmission = "Transmission";
    public const string DriveType = "DriveType";
    public const string Brake = "Brake";
}

public interface IVehicleCatalogService
{
    Task<IReadOnlyList<int>> GetYearsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetMakesAsync(
        int year,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetModelsAsync(
        int year,
        string make,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetOptionsAsync(
        string category,
        CancellationToken cancellationToken = default);
}

using TPGLLC.Data.Entities;

namespace TPGLLC.Services.Vehicles;

public interface IVehicleCatalogService
{
    Task<IReadOnlyList<string>> GetVehicleTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetYearsAsync(
        string vehicleType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetMakesAsync(
        string vehicleType,
        int year,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetModelsAsync(
        string vehicleType,
        int year,
        string make,
        CancellationToken cancellationToken = default);
}
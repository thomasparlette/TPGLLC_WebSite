using TPGLLC.Data.Entities;

namespace TPGLLC.Services.Vehicles;

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
}
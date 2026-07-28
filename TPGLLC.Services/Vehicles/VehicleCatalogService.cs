using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TPGLLC.Data;

namespace TPGLLC.Services.Vehicles;

public sealed class VehicleCatalogService : IVehicleCatalogService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly TPGLLCDbContext _db;
    private readonly IMemoryCache _cache;

    public VehicleCatalogService(TPGLLCDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }


    public async Task<IReadOnlyList<int>> GetYearsAsync(
        CancellationToken cancellationToken = default)
    {
       var key = $"vehicle-years";

        if (_cache.TryGetValue(key, out IReadOnlyList<int>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _db.VehicleCatalogEntries
            .AsNoTracking()
            .Select(x => x.ModelYear)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync(cancellationToken);

        if (result.Count > 0)
        {
            _cache.Set(key, result, CacheDuration);
        }

        return result;
    }

    public async Task<IReadOnlyList<string>> GetMakesAsync(
        int year,
        CancellationToken cancellationToken = default)
    {

        var key = $"vehicle-makes:{year}";

        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _db.VehicleCatalogEntries
            .AsNoTracking()
            .Where(x =>  x.ModelYear == year)
            .Select(x => x.Make)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (result.Count > 0)
        {
            _cache.Set(key, result, CacheDuration);
        }

        return result;
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(
        int year,
        string make,
        CancellationToken cancellationToken = default)
    {
       

        var key = $"vehicle-models:{year}:{make}";

        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _db.VehicleCatalogEntries
            .AsNoTracking()
            .Where(x =>
                x.ModelYear == year &&
                x.Make == make)
            .Select(x => x.Model)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (result.Count > 0)
        {
            _cache.Set(key, result, CacheDuration);
        }

        return result;
    }
}
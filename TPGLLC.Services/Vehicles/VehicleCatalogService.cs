using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TPGLLC.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace TPGLLC.Services.Vehicles;

public sealed class VehicleCatalogService : IVehicleCatalogService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly TPGLLCDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VehicleCatalogService>? _logger;

    public VehicleCatalogService(TPGLLCDbContext db, IMemoryCache cache, ILogger<VehicleCatalogService>? logger = null)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
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

    public async Task<IReadOnlyList<string>> GetOptionsAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var normalizedCategory = category?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedCategory))
        {
            return [];
        }

        var key = $"vehicle-options:{normalizedCategory}";

        if (_cache.TryGetValue(key, out IReadOnlyList<string>? cached) && cached is not null)
        {
            return cached;
        }

        List<string> result;
        try
        {
            result = await _db.VehicleCatalogOptions
            .AsNoTracking()
            .Where(x => x.Category == normalizedCategory)
            .Select(x => x.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            // This optional catalog was absent in older databases. Callers already
            // provide standard choices for an empty result; keep that path usable.
            _logger?.LogWarning(ex, "Optional VehicleCatalogOptions table is unavailable; using standard vehicle choices.");
            IReadOnlyList<string> fallback = Array.Empty<string>();
            _cache.Set(key, fallback, TimeSpan.FromMinutes(1));
            return fallback;
        }

        if (result.Count > 0)
        {
            _cache.Set(key, result, CacheDuration);
        }

        return result;
    }
}

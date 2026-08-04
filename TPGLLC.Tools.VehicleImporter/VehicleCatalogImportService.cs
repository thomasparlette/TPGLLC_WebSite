using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TPGLLC.Data;
using TPGLLC.Data.Entities;

namespace TPGLLC.Tools.VehicleImporter;

public sealed class VehicleCatalogImportService
{
    private const int BatchSize = 500;

    private static readonly HashSet<string> MotorcycleMakes = new(StringComparer.OrdinalIgnoreCase)
    {
        "CFMOTO",
        "Can-Am",
        "Ducati",
        "Gas Gas",
        "Harley-Davidson",
        "Husqvarna",
        "Indian",
        "KTM",
        "Kawasaki",
        "Polaris",
        "Suzuki",
        "Triumph",
        "Vespa",
        "Yamaha",
    };

    private readonly TPGLLCDbContext _db;
    private readonly IVpicApiClient _vpic;
    private readonly ILogger<VehicleCatalogImportService> _logger;
    private readonly VehicleImportSettings _settings;

    public VehicleCatalogImportService(
        TPGLLCDbContext db,
        IVpicApiClient vpic,
        IOptions<VehicleImportSettings> settings,
        ILogger<VehicleCatalogImportService> logger)
    {
        _db = db;
        _vpic = vpic;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        var startYear = _settings.StartYear;
        var endYear = _settings.EndYear;
        var vehicleType = NormalizeCatalogValue(_settings.VehicleType);

        _logger.LogInformation(
            "Starting vPIC import for years {StartYear} through {EndYear}. VehicleType={VehicleType}",
            startYear,
            endYear,
            vehicleType);

        await _db.Database.MigrateAsync(cancellationToken);
        await DeleteExistingSliceAsync(startYear, endYear, cancellationToken);

        var allowedMakes = NormalizeAllowedMakes(_settings.AllowedMakes);
        if (allowedMakes.Count == 0)
        {
            throw new InvalidOperationException("VehicleImport:AllowedMakes is empty.");
        }

        var allMakes = await _vpic.GetAllMakesAsync(cancellationToken);
        var makeLookup = allMakes
            .Where(x => !string.IsNullOrWhiteSpace(x.MakeName))
            .GroupBy(x => NormalizeCatalogValue(x.MakeName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.MakeId).First(),
                StringComparer.OrdinalIgnoreCase);

        var resolvedMakes = new List<VpicMakeDto>();
        var lookupFailures = 0;

        foreach (var allowedMake in allowedMakes)
        {
            if (makeLookup.TryGetValue(allowedMake, out var make))
            {
                resolvedMakes.Add(make);
                continue;
            }

            lookupFailures++;
            _logger.LogWarning("Allowed make not found in vPIC: {AllowedMake}", allowedMake);
        }

        if (resolvedMakes.Count == 0)
        {
            throw new InvalidOperationException("No allowed makes were resolved from vPIC.");
        }

        var years = Enumerable.Range(startYear, endYear - startYear + 1).ToArray();
        var seenThisRun = new ConcurrentDictionary<VehicleCatalogKey, byte>();
        var newRows = new ConcurrentBag<VehicleCatalogEntry>();

        var skippedDuplicateSource = 0;
        var failedRequests = 0;

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism > 0
                ? _settings.MaxDegreeOfParallelism
                : Environment.ProcessorCount
        };

        await Parallel.ForEachAsync(resolvedMakes, parallelOptions, async (make, ct) =>
        {
            var makeVehicleType = ResolveVehicleType(make.MakeName, vehicleType);

            foreach (var year in years)
            {
                IReadOnlyList<VpicModelDto> models;
                try
                {
                    models = await _vpic.GetModelsForMakeIdYearAsync(make.MakeId, year, ct);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedRequests);
                    _logger.LogWarning(
                        ex,
                        "vPIC lookup failed for make {Make} ({MakeId}) year {Year}.",
                        make.MakeName,
                        make.MakeId,
                        year);
                    continue;
                }

                if (models.Count == 0)
                {
                    continue;
                }

                foreach (var model in models)
                {
                    if (string.IsNullOrWhiteSpace(model.ModelName))
                    {
                        continue;
                    }

                    var key = new VehicleCatalogKey(makeVehicleType, year, make.MakeId, model.ModelId);

                    if (!seenThisRun.TryAdd(key, 0))
                    {
                        Interlocked.Increment(ref skippedDuplicateSource);
                        continue;
                    }

                    newRows.Add(new VehicleCatalogEntry
                    {
                        ModelYear = year,
                        MakeId = make.MakeId,
                        ModelId = model.ModelId,
                        Make = Truncate(NormalizeCatalogValue(model.MakeName), 120),
                        Model = Truncate(NormalizeCatalogValue(model.ModelName), 120),
                        SyncedAtUtc = DateTimeOffset.UtcNow
                    });
                }
            }
        });

        var imported = await FlushBatchesAsync(newRows.ToList(), cancellationToken);

        _logger.LogInformation(
            "vPIC import complete. Imported {Imported} new rows. Skipped {SkippedDuplicateSource} duplicate source rows. Lookup failures: {LookupFailures}. Request failures: {FailedRequests}.",
            imported,
            skippedDuplicateSource,
            lookupFailures,
            failedRequests);
    }

    private void ValidateSettings()
    {
        if (_settings.StartYear < 1900)
        {
            throw new InvalidOperationException("VehicleImport:StartYear must be 1900 or later.");
        }

        if (_settings.EndYear < _settings.StartYear)
        {
            throw new InvalidOperationException("VehicleImport:EndYear must be greater than or equal to StartYear.");
        }

        if (_settings.MaxDegreeOfParallelism < 1)
        {
            throw new InvalidOperationException("VehicleImport:MaxDegreeOfParallelism must be at least 1.");
        }
    }

    private async Task DeleteExistingSliceAsync(
        int startYear,
        int endYear,
        CancellationToken cancellationToken)
    {
        var existingRows = await _db.VehicleCatalogEntries
            .Where(x => x.ModelYear >= startYear && x.ModelYear <= endYear)
            .ToListAsync(cancellationToken);

        if (existingRows.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Removing {Count} existing catalog rows for years {StartYear}-{EndYear} before refresh.",
            existingRows.Count,
            startYear,
            endYear);

        _db.VehicleCatalogEntries.RemoveRange(existingRows);
        await _db.SaveChangesAsync(cancellationToken);
        _db.ChangeTracker.Clear();
    }

    private async Task<int> FlushBatchesAsync(
        List<VehicleCatalogEntry> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        var imported = 0;

        for (var index = 0; index < rows.Count; index += BatchSize)
        {
            var batch = rows.Skip(index).Take(BatchSize).ToList();
            imported += await FlushAsync(batch, cancellationToken);
        }

        return imported;
    }

    private async Task<int> FlushAsync(List<VehicleCatalogEntry> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        try
        {
            await _db.VehicleCatalogEntries.AddRangeAsync(rows, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return rows.Count;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(
                ex,
                "Batch insert of {Count} catalog rows failed; retrying one row at a time.",
                rows.Count);

            _db.ChangeTracker.Clear();

            var inserted = 0;

            foreach (var row in rows)
            {
                try
                {
                    await _db.VehicleCatalogEntries.AddAsync(row, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                    inserted++;
                }
                catch (DbUpdateException rowEx)
                {
                    _logger.LogWarning(
                        rowEx,
                        "Skipping bad catalog row {VehicleType} {Year} {MakeId}/{ModelId} {Make} {Model}.",
                        row.ModelYear,
                        row.MakeId,
                        row.ModelId,
                        row.Make,
                        row.Model);

                    _db.ChangeTracker.Clear();
                }
            }

            return inserted;
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }
    }

    private static List<string> NormalizeAllowedMakes(IEnumerable<string>? makes)
    {
        if (makes is null)
        {
            return [];
        }

        return makes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeCatalogValue)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static string ResolveVehicleType(string makeName, string defaultVehicleType)
    {
        if (MotorcycleMakes.Contains(makeName))
        {
            return "Motorcycle";
        }

        return string.IsNullOrWhiteSpace(defaultVehicleType)
            ? "Automotive"
            : defaultVehicleType.Trim();
    }

    private static string NormalizeCatalogValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(" ", parts);
    }

    private static string Truncate(string? value, int maxLength)
    {
        var normalized = NormalizeCatalogValue(value);

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private readonly record struct VehicleCatalogKey(
        string VehicleType,
        int ModelYear,
        int MakeId,
        int ModelId);
}

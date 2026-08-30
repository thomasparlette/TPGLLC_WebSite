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

    private readonly TPGLLCDbContext _db;
    private readonly IVpicApiClient _vpic;
    private readonly VehicleImportSettings _settings;
    private readonly ILogger<VehicleCatalogImportService> _logger;

    public VehicleCatalogImportService(
        TPGLLCDbContext db,
        IVpicApiClient vpic,
        IOptions<VehicleImportSettings> settings,
        ILogger<VehicleCatalogImportService> logger)
    {
        _db = db;
        _vpic = vpic;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        var allowedMakes = _settings.AllowedMakes
            .Select(Normalize)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allowedMakes.Count == 0)
        {
            throw new InvalidOperationException("AllowedMakes must contain at least one make.");
        }

        _logger.LogInformation(
            "Starting vPIC import for years {StartYear} through {EndYear} with {MakeCount} allowed makes.",
            _settings.StartYear,
            _settings.EndYear,
            allowedMakes.Count);

        await _db.Database.MigrateAsync(cancellationToken);
        await SyncOptionCatalogAsync(cancellationToken);

        var existingKeys = _settings.ReplaceExisting
            ? new HashSet<VehicleCatalogKey>()
            : await LoadExistingKeysAsync(cancellationToken);

        var collected = await FetchRowsAsync(allowedMakes, cancellationToken);

        if (collected.Count == 0)
        {
            throw new InvalidOperationException("No catalog rows were imported from vPIC.");
        }

        var rows = collected.Values.ToList();

        if (!_settings.ReplaceExisting)
        {
            rows = rows
                .Where(row => !existingKeys.Contains(new VehicleCatalogKey(row.ModelYear, row.MakeId, row.ModelId)))
                .ToList();
        }

        if (rows.Count == 0)
        {
            _logger.LogInformation("All imported rows already exist locally. Nothing to insert.");
            return;
        }

        var executionStrategy = _db.Database.CreateExecutionStrategy();

        var inserted = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (_settings.ReplaceExisting)
                {
                    var deleted = await _db.VehicleCatalogEntries
                        .Where(x =>
                            x.ModelYear >= _settings.StartYear &&
                            x.ModelYear <= _settings.EndYear)
                        .ExecuteDeleteAsync(cancellationToken);

                    _logger.LogInformation(
                        "Removed {DeletedCount} existing catalog rows for years {StartYear} through {EndYear}.",
                        deleted,
                        _settings.StartYear,
                        _settings.EndYear);
                }

                var count = await FlushAsync(rows, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return count;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        _logger.LogInformation(
            "vPIC import complete. Imported {Imported} rows for {MakeCount} makes across {StartYear}-{EndYear}.",
            inserted,
            allowedMakes.Count,
            _settings.StartYear,
            _settings.EndYear);
    }

    private async Task<ConcurrentDictionary<VehicleCatalogKey, VehicleCatalogEntry>> FetchRowsAsync(IReadOnlyList<string> allowedMakes, CancellationToken cancellationToken)
    {
        var years = Enumerable.Range(_settings.StartYear, _settings.EndYear - _settings.StartYear + 1).ToArray();
        var rows = new ConcurrentDictionary<VehicleCatalogKey, VehicleCatalogEntry>();
        var failedLookups = 0;
        var duplicateSourceRows = 0;

        var workItems = years
            .SelectMany(year => allowedMakes.Select(make => new ImportWorkItem(year, make)))
            .ToArray();

        await Parallel.ForEachAsync(
            workItems,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Max(1, _settings.MaxDegreeOfParallelism)
            },
            async (item, token) =>
            {
                try
                {
                    var models = await _vpic.GetModelsForMakeYearAsync(item.Make, item.Year, token);

                    foreach (var model in models)
                    {
                        var makeName = Normalize(string.IsNullOrWhiteSpace(model.MakeName) ? item.Make : model.MakeName);
                        if (!makeName.Equals(item.Make, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var key = new VehicleCatalogKey(item.Year, model.MakeId, model.ModelId);
                        var row = new VehicleCatalogEntry
                        {
                            ModelYear = item.Year,
                            MakeId = model.MakeId,
                            ModelId = model.ModelId,
                            Make = makeName,
                            Model = Normalize(model.ModelName),
                            SyncedAtUtc = DateTimeOffset.UtcNow
                        };

                        if (!rows.TryAdd(key, row))
                        {
                            Interlocked.Increment(ref duplicateSourceRows);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedLookups);
                    _logger.LogWarning(
                        ex,
                        "Lookup failed for make {Make} year {Year}.",
                        item.Make,
                        item.Year);
                }
            });

        _logger.LogInformation(
            "Resolved {ImportedCount} candidate rows. Duplicate source rows: {DuplicateRows}. Failed lookups: {FailedLookups}.",
            rows.Count,
            duplicateSourceRows,
            failedLookups);

        return rows;
    }

    private async Task SyncOptionCatalogAsync(CancellationToken cancellationToken)
    {
        var configured = new Dictionary<string, IReadOnlyCollection<string>>
        {
            [VehicleCatalogOptionCategories.Submodel] = _settings.CatalogOptions.Submodels,
            [VehicleCatalogOptionCategories.BodyStyle] = _settings.CatalogOptions.BodyStyles,
            [VehicleCatalogOptionCategories.EngineFuel] = _settings.CatalogOptions.EngineFuelTypes,
            [VehicleCatalogOptionCategories.Transmission] = _settings.CatalogOptions.Transmissions,
            [VehicleCatalogOptionCategories.DriveType] = _settings.CatalogOptions.DriveTypes,
            [VehicleCatalogOptionCategories.Brake] = _settings.CatalogOptions.Brakes
        };

        var existing = await _db.VehicleCatalogOptions
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        foreach (var category in configured)
        {
            var values = category.Value
                .Select(Normalize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var value in values)
            {
                var row = existing.FirstOrDefault(x =>
                    x.Category.Equals(category.Key, StringComparison.OrdinalIgnoreCase) &&
                    x.Value.Equals(value, StringComparison.OrdinalIgnoreCase));

                if (row is null)
                {
                    _db.VehicleCatalogOptions.Add(new VehicleCatalogOption
                    {
                        Category = category.Key,
                        Value = value,
                        Source = "vPIC",
                        IsActive = true,
                        SyncedAtUtc = now
                    });
                }
                else
                {
                    row.Source = "vPIC";
                    row.IsActive = true;
                    row.SyncedAtUtc = now;
                }
            }

            foreach (var row in existing.Where(x =>
                         x.Category.Equals(category.Key, StringComparison.OrdinalIgnoreCase) &&
                         !values.Contains(x.Value)))
            {
                row.IsActive = false;
                row.SyncedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Vehicle option catalog synchronized with {OptionCount} configured values.",
            configured.Values.Sum(values => values.Count));
    }

    private async Task<HashSet<VehicleCatalogKey>> LoadExistingKeysAsync(CancellationToken cancellationToken)
    {
        var keys = await _db.VehicleCatalogEntries
            .AsNoTracking()
            .Select(x => new VehicleCatalogKey(
                x.ModelYear,
                x.MakeId,
                x.ModelId))
            .ToListAsync(cancellationToken);

        return new HashSet<VehicleCatalogKey>(keys);
    }

    private async Task<int> FlushAsync(List<VehicleCatalogEntry> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        var inserted = 0;

        for (var index = 0; index < rows.Count; index += BatchSize)
        {
            var batch = rows
                .Skip(index)
                .Take(BatchSize)
                .ToList();

            await _db.VehicleCatalogEntries.AddRangeAsync(batch, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            inserted += batch.Count;
            _db.ChangeTracker.Clear();
        }

        return inserted;
    }

    private void ValidateSettings()
    {
        if (_settings.StartYear < 1900 || _settings.StartYear > 3000)
        {
            throw new InvalidOperationException("Vehicle import StartYear is invalid.");
        }

        if (_settings.EndYear < _settings.StartYear || _settings.EndYear > 3000)
        {
            throw new InvalidOperationException("Vehicle import EndYear is invalid.");
        }

        if (_settings.MaxDegreeOfParallelism < 1)
        {
            throw new InvalidOperationException("Vehicle import MaxDegreeOfParallelism must be at least 1.");
        }
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private readonly record struct ImportWorkItem(int Year, string Make);

    private readonly record struct VehicleCatalogKey(
        int ModelYear,
        int MakeId,
        int ModelId);
}

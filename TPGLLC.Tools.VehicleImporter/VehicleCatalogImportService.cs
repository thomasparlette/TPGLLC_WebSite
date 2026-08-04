using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TPGLLC.Data;
using TPGLLC.Data.Entities;

namespace TPGLLC.Tools.VehicleImporter;

public sealed class VehicleCatalogImportService
{
    private const int StartYear = 1996;
    private const int BatchSize = 500;

    private readonly TPGLLCDbContext _db;
    private readonly IVpicApiClient _vpic;
    private readonly ILogger<VehicleCatalogImportService> _logger;

    private static readonly HashSet<string> AllowedMakes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Acura","Alfa Romeo","Aston Martin","Audi","BMW","Buick","Bentley","Bugatti","Buick","Cadillac","Chevrolet","Chrysler",
        "Dodge","Ferrari","Fiat","Ford","GMC","Hino","Honda","Hyundai","Infiniti","Isuzu","Jaguar","Jeep","Kia",
        "Lexus","Lincoln"," Mazda","Mercedes-Benc","Mitsubishi","Nissan",
        "Lamborghini","Land Rover","Lexus","Lincoln","Lotus","Lucid","Maserati","Mazda","McLaren","Mercedes-Benz","Mini","Mitsubishi","Nissan","Porsche",
        "Ram","Rivian","Rolls-Royce","Subaru","Tesla","Toyota","Volkswagen","Volvo",

        "CFMOTO","Can-Am","Ducati","Gas Gas","Harley-Davidson","Husqvarna","Indian","KTM","Kawasaki","Polaris","Suzuki", "Triumph","Vespa", "Yamaha",
    };


    public VehicleCatalogImportService(
        TPGLLCDbContext db,
        IVpicApiClient vpic,
        ILogger<VehicleCatalogImportService> logger)
    {
        _db = db;
        _vpic = vpic;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var endYear = DateTime.UtcNow.Year;

        _logger.LogInformation(
            "Starting vPIC import for years {StartYear} through {EndYear}",
            StartYear,
            endYear);

        await _db.Database.MigrateAsync(cancellationToken);

        var existingKeys = await LoadExistingKeysAsync(cancellationToken);
        _logger.LogInformation("Loaded {Count} existing catalog keys.", existingKeys.Count);

        var seenThisRun = new HashSet<VehicleCatalogKey>();
        var pending = new List<VehicleCatalogEntry>(BatchSize);

        var imported = 0;
        var skippedExisting = 0;
        var skippedDuplicateSource = 0;
        var lookupFailures = 0;

        if (pending.Count > 0)
        {
            imported += await FlushAsync(pending, cancellationToken);
            pending.Clear();
        }

        _logger.LogInformation(
            "vPIC import complete. Imported {Imported} new rows. Skipped {SkippedExisting} existing rows. Skipped {SkippedDuplicateSource} duplicate source rows. Lookup failures: {LookupFailures}.",
            imported,
            skippedExisting,
            skippedDuplicateSource,
            lookupFailures);
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
                        "Skipping bad catalog row {Year} {MakeId}/{ModelId} {Make} {Model}.",
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

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value.Trim();

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }

    private readonly record struct VehicleCatalogKey(
        int ModelYear,
        int MakeId,
        int ModelId);
}

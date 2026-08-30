using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public sealed class EstimateCatalogService : IEstimateCatalogService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EstimateCatalogService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbFactory = dbFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<EstimateCatalogPageViewModel> GetAsync()
    {
        if (!IsStaff())
        {
            return new EstimateCatalogPageViewModel
            {
                ErrorMessage = "You are not authorized to manage the estimate catalog."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var model = new EstimateCatalogPageViewModel
        {
            Parts = await db.PartsCatalogItems
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .ToListAsync(),
            Labor = await db.LaborCatalogItems
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .ToListAsync()
        };

        return model;
    }

    public async Task<EstimateCatalogPageViewModel> SavePartAsync(EstimateCatalogPageViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!IsStaff())
        {
            model.ErrorMessage = "You are not authorized to manage the estimate catalog.";
            return model;
        }

        if (!TryParseDecimal(model.PartForm.UnitPrice, out var unitPrice))
        {
            model.ErrorMessage = "Retail price must be a valid non-negative number.";
            return model;
        }

        if (!TryParseOptionalDecimal(model.PartForm.UnitCost, out var unitCost))
        {
            model.ErrorMessage = "Unit cost must be a valid non-negative number.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var partNumber = model.PartForm.PartNumber.Trim();
        var duplicate = await db.PartsCatalogItems
            .AsNoTracking()
            .AnyAsync(x => x.Id != model.PartForm.Id && x.PartNumber.ToUpper() == partNumber.ToUpper());
        if (duplicate)
        {
            model.ErrorMessage = $"Part number '{partNumber}' is already in the catalog.";
            return model;
        }

        var item = model.PartForm.Id == Guid.Empty
            ? new PartsCatalogItem()
            : await db.PartsCatalogItems.FirstOrDefaultAsync(x => x.Id == model.PartForm.Id);

        if (item is null)
        {
            model.ErrorMessage = "The selected catalog part was not found.";
            return model;
        }

        item.PartNumber = partNumber;
        item.Name = model.PartForm.Name.Trim();
        item.Description = Normalize(model.PartForm.Description);
        item.UnitCost = unitCost;
        item.UnitPrice = unitPrice!.Value;
        item.IsActive = model.PartForm.IsActive;
        item.UpdatedUtc = DateTimeOffset.UtcNow;

        if (model.PartForm.Id == Guid.Empty)
        {
            db.PartsCatalogItems.Add(item);
        }

        await db.SaveChangesAsync();
        var result = await GetAsync();
        result.SuccessMessage = $"Part '{item.Name}' saved.";
        return result;
    }

    public async Task<EstimateCatalogPageViewModel> SaveLaborAsync(EstimateCatalogPageViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!IsStaff())
        {
            model.ErrorMessage = "You are not authorized to manage the estimate catalog.";
            return model;
        }

        if (!TryParseDecimal(model.LaborForm.DefaultHours, out var defaultHours) || defaultHours <= 0)
        {
            model.ErrorMessage = "Standard hours must be greater than zero.";
            return model;
        }

        if (!TryParseDecimal(model.LaborForm.HourlyRate, out var hourlyRate))
        {
            model.ErrorMessage = "Hourly rate must be a valid non-negative number.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var code = model.LaborForm.Code.Trim();
        var duplicate = await db.LaborCatalogItems
            .AsNoTracking()
            .AnyAsync(x => x.Id != model.LaborForm.Id && x.Code.ToUpper() == code.ToUpper());
        if (duplicate)
        {
            model.ErrorMessage = $"Labor code '{code}' is already in the catalog.";
            return model;
        }

        var item = model.LaborForm.Id == Guid.Empty
            ? new LaborCatalogItem()
            : await db.LaborCatalogItems.FirstOrDefaultAsync(x => x.Id == model.LaborForm.Id);

        if (item is null)
        {
            model.ErrorMessage = "The selected labor operation was not found.";
            return model;
        }

        item.Code = code;
        item.Name = model.LaborForm.Name.Trim();
        item.Description = Normalize(model.LaborForm.Description);
        item.DefaultHours = defaultHours!.Value;
        item.HourlyRate = hourlyRate!.Value;
        item.IsActive = model.LaborForm.IsActive;
        item.UpdatedUtc = DateTimeOffset.UtcNow;

        if (model.LaborForm.Id == Guid.Empty)
        {
            db.LaborCatalogItems.Add(item);
        }

        await db.SaveChangesAsync();
        var result = await GetAsync();
        result.SuccessMessage = $"Labor operation '{item.Name}' saved.";
        return result;
    }

    private bool IsStaff()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole("ServiceAdvisor") == true || user?.IsInRole("Administrator") == true;
    }

    private static bool TryParseDecimal(string? value, out decimal? result)
    {
        result = null;
        var normalized = value?.Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0)
        {
            return false;
        }

        result = parsed;
        return true;
    }

    private static bool TryParseOptionalDecimal(string? value, out decimal? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        return TryParseDecimal(value, out result);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

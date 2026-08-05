using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Services.Vehicles;
using TPGLLC.Web.Components.PortalShared.Vehicles;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public sealed class VehiclePortalService : IVehiclePortalService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly ICustomerProfileService _customerProfileService;
    private readonly IVehicleCatalogService _vehicleCatalogService;

    public VehiclePortalService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        ICustomerProfileService customerProfileService,
        IVehicleCatalogService vehicleCatalogService)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _customerProfileService = customerProfileService;
        _vehicleCatalogService = vehicleCatalogService;
    }

    public async Task<VehiclePageViewModel> GetAsync()
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new VehiclePageViewModel
            {
                ErrorMessage = "You must be signed in to manage vehicles."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var customer = await EnsureCustomerAsync(db, current);

        var vehicles = await db.CustomerVehicles
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Id)
            .OrderByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        return new VehiclePageViewModel
        {
            Vehicles = vehicles,
            Years = await GetYearsAsync(),
            Form = new VehicleFormModel()
        };
    }

    public async Task<VehiclePageViewModel> StartEditAsync(Guid vehicleId)
    {
        var model = await GetAsync();
        if (!string.IsNullOrWhiteSpace(model.ErrorMessage))
        {
            return model;
        }

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        await using var db = await _dbFactory.CreateDbContextAsync();

        var vehicle = await db.CustomerVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.Customer.ApplicationUserId == current.UserId);

        if (vehicle is null)
        {
            model.ErrorMessage = "Vehicle not found.";
            return model;
        }

        model.EditingVehicleId = vehicle.Id;
        model.Form = new VehicleFormModel
        {
            ModelYear = vehicle.ModelYear?.ToString() ?? string.Empty,
            Make = vehicle.Make ?? string.Empty,
            Model = vehicle.Model ?? string.Empty,
            Vin = vehicle.Vin,
            Nickname = vehicle.Nickname,
            LicensePlate = vehicle.LicensePlate,
            Mileage = vehicle.Mileage?.ToString(),
            IsPrimary = vehicle.IsPrimary
        };

        if (int.TryParse(model.Form.ModelYear, out var year))
        {
            model.Makes = (await _vehicleCatalogService.GetMakesAsync(year)).ToList();
        }

        if (int.TryParse(model.Form.ModelYear, out var year2) &&
            !string.IsNullOrWhiteSpace(model.Form.Make))
        {
            model.Models = (await _vehicleCatalogService.GetModelsAsync(year2, model.Form.Make)).ToList();
        }

        return model;
    }

    public async Task<VehiclePageViewModel> ResetAsync()
    {
        var model = await GetAsync();
        model.EditingVehicleId = null;
        model.Form = new VehicleFormModel();
        model.Makes = [];
        model.Models = [];
        model.SuccessMessage = null;
        model.ErrorMessage = null;
        return model;
    }

    public async Task<VehiclePageViewModel> YearChangedAsync(VehiclePageViewModel model)
    {
        model.Form.Make = string.Empty;
        model.Form.Model = string.Empty;
        model.Makes = [];
        model.Models = [];

        if (int.TryParse(model.Form.ModelYear, out var year))
        {
            model.Makes = (await _vehicleCatalogService.GetMakesAsync(year)).ToList();
        }

        return model;
    }

    public async Task<VehiclePageViewModel> MakeChangedAsync(VehiclePageViewModel model)
    {
        model.Form.Model = string.Empty;
        model.Models = [];

        if (int.TryParse(model.Form.ModelYear, out var year) &&
            !string.IsNullOrWhiteSpace(model.Form.Make))
        {
            model.Models = (await _vehicleCatalogService.GetModelsAsync(year, model.Form.Make)).ToList();
        }

        return model;
    }

    public async Task<VehiclePageViewModel> SaveAsync(VehiclePageViewModel model)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            model.ErrorMessage = "You must be signed in to manage vehicles.";
            return model;
        }

        if (!TryParseYear(model.Form.ModelYear, out var modelYear))
        {
            model.ErrorMessage = "Model year must be a valid year.";
            return model;
        }

        if (!TryParseMileage(model.Form.Mileage, out var mileage))
        {
            model.ErrorMessage = "Mileage must be a valid whole number.";
            return model;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var customer = await EnsureCustomerAsync(db, current);

        CustomerVehicle vehicle;
        var isNew = model.EditingVehicleId is null;

        if (isNew)
        {
            vehicle = new CustomerVehicle
            {
                CustomerId = customer.Id,
                CreatedUtc = DateTimeOffset.UtcNow
            };
            db.CustomerVehicles.Add(vehicle);
        }
        else
        {
            vehicle = await db.CustomerVehicles
                .FirstOrDefaultAsync(x => x.Id == model.EditingVehicleId && x.CustomerId == customer.Id)
                ?? throw new InvalidOperationException("Vehicle not found.");
        }

        vehicle.ModelYear = modelYear;
        vehicle.Make = string.IsNullOrWhiteSpace(model.Form.Make) ? null : model.Form.Make.Trim();
        vehicle.Model = string.IsNullOrWhiteSpace(model.Form.Model) ? null : model.Form.Model.Trim();
        vehicle.Vin = string.IsNullOrWhiteSpace(model.Form.Vin) ? null : model.Form.Vin.Trim();
        vehicle.Nickname = string.IsNullOrWhiteSpace(model.Form.Nickname) ? null : model.Form.Nickname.Trim();
        vehicle.LicensePlate = string.IsNullOrWhiteSpace(model.Form.LicensePlate) ? null : model.Form.LicensePlate.Trim();
        vehicle.Mileage = mileage;
        vehicle.IsPrimary = model.Form.IsPrimary;
        vehicle.UpdatedUtc = DateTimeOffset.UtcNow;

        if (vehicle.IsPrimary)
        {
            var others = await db.CustomerVehicles
                .Where(x => x.CustomerId == customer.Id && x.Id != vehicle.Id && x.IsPrimary)
                .ToListAsync();

            foreach (var other in others)
            {
                other.IsPrimary = false;
                other.UpdatedUtc = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();

        var refreshed = await GetAsync();
        refreshed.SuccessMessage = isNew ? "Vehicle added." : "Vehicle updated.";
        return refreshed;
    }

    public async Task<VehiclePageViewModel> DeleteAsync(Guid vehicleId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new VehiclePageViewModel { ErrorMessage = "You must be signed in to manage vehicles." };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var vehicle = await db.CustomerVehicles
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.Customer.ApplicationUserId == current.UserId);

        if (vehicle is null)
        {
            return await GetAsync();
        }

        var wasPrimary = vehicle.IsPrimary;
        db.CustomerVehicles.Remove(vehicle);
        await db.SaveChangesAsync();

        if (wasPrimary)
        {
            var fallback = await db.CustomerVehicles
                .Where(x => x.Customer.ApplicationUserId == current.UserId)
                .OrderByDescending(x => x.CreatedUtc)
                .FirstOrDefaultAsync();

            if (fallback is not null)
            {
                fallback.IsPrimary = true;
                fallback.UpdatedUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        var refreshed = await GetAsync();
        refreshed.SuccessMessage = "Vehicle deleted.";
        return refreshed;
    }

    public async Task<VehiclePageViewModel> MakePrimaryAsync(Guid vehicleId)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new VehiclePageViewModel { ErrorMessage = "You must be signed in to manage vehicles." };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var selected = await db.CustomerVehicles
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.Customer.ApplicationUserId == current.UserId);

        if (selected is null)
        {
            return await GetAsync();
        }

        var others = await db.CustomerVehicles
            .Where(x => x.Customer.ApplicationUserId == current.UserId && x.Id != vehicleId && x.IsPrimary)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsPrimary = false;
            other.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        selected.IsPrimary = true;
        selected.UpdatedUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        var refreshed = await GetAsync();
        refreshed.SuccessMessage = "Primary vehicle updated.";
        return refreshed;
    }

    private async Task<List<int>> GetYearsAsync()
    {
        var years = await _vehicleCatalogService.GetYearsAsync();
        var list = years.Distinct().OrderByDescending(x => x).ToList();

        if (list.Count == 0)
        {
            list = Enumerable.Range(1995, DateTime.UtcNow.Year - 1995 + 1).Reverse().ToList();
        }

        return list;
    }

    private static bool TryParseYear(string? value, out int? year)
    {
        year = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (int.TryParse(value.Trim(), out var parsed) && parsed is >= 1900 and <= 3000)
        {
            year = parsed;
            return true;
        }

        return false;
    }

    private static bool TryParseMileage(string? value, out int? mileage)
    {
        mileage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Replace(",", string.Empty, StringComparison.Ordinal).Trim();
        if (int.TryParse(normalized, out var parsed) && parsed >= 0)
        {
            mileage = parsed;
            return true;
        }

        return false;
    }

    private async Task<Customer> EnsureCustomerAsync(TPGLLCDbContext db, CurrentCustomer current)
    {
        var existing = await db.Customers
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        if (existing is not null)
        {
            return existing;
        }

        var profile = await _customerProfileService.GetCurrentAsync();

        var customer = new Customer
        {
            ApplicationUserId = current.UserId,
            FirstName = string.IsNullOrWhiteSpace(profile?.FirstName) ? "Customer" : profile!.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(profile?.LastName) ? "Account" : profile!.LastName.Trim(),
            Email = string.IsNullOrWhiteSpace(current.Email) ? null : current.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(profile?.Phone) ? null : profile.Phone.Trim(),
            AddressLine1 = string.IsNullOrWhiteSpace(profile?.Address1) ? null : profile.Address1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(profile?.Address2) ? null : profile.Address2.Trim(),
            City = string.IsNullOrWhiteSpace(profile?.City) ? null : profile.City.Trim(),
            State = string.IsNullOrWhiteSpace(profile?.State) ? null : profile.State.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(profile?.ZipCode) ? null : profile.ZipCode.Trim(),
            Notes = null,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }
}

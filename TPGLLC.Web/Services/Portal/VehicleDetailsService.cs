using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public sealed class VehicleDetailsService : IVehicleDetailsService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly IBuildEnvironmentService _buildEnvironmentService;

    public VehicleDetailsService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IBuildEnvironmentService buildEnvironmentService)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _buildEnvironmentService = buildEnvironmentService;
    }

    public async Task<VehicleDetailsViewModel> GetAsync(Guid vehicleId)
    {
        if (_buildEnvironmentService.IsBuildEnvironment)
        {
            return CreateDemoModel(vehicleId);
        }

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new VehicleDetailsViewModel
            {
                ErrorMessage = "You must be signed in to view vehicle details."
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.ChangeTracker.Clear();

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        if (customer is null)
        {
            return new VehicleDetailsViewModel
            {
                ErrorMessage = "Customer record not found."
            };
        }

        var vehicle = await db.CustomerVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.CustomerId == customer.Id);

        if (vehicle is null)
        {
            return new VehicleDetailsViewModel
            {
                ErrorMessage = "Vehicle not found."
            };
        }

        var history = await db.ServiceHistoryEntries
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Id && x.CustomerVehicleId == vehicle.Id)
            .OrderByDescending(x => x.ServiceDate)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync();

        return new VehicleDetailsViewModel
        {
            Vehicle = vehicle,
            ServiceHistory = history
        };
    }

    private static VehicleDetailsViewModel CreateDemoModel(Guid vehicleId)
    {
        var vehicle = new CustomerVehicle
        {
            Id = vehicleId,
            CustomerId = Guid.NewGuid(),
            ModelYear = 2016,
            Make = "Acura",
            Model = "MDX",
            Nickname = "Family SUV",
            LicensePlate = "DEMO-1",
            Mileage = 84_521,
            IsPrimary = true,
            CreatedUtc = DateTimeOffset.UtcNow.AddDays(-12)
        };

        return new VehicleDetailsViewModel
        {
            Vehicle = vehicle,
            ServiceHistory =
            [
                new ServiceHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    CustomerId = vehicle.CustomerId,
                    CustomerVehicleId = vehicle.Id,
                    VehicleName = "2016 Acura MDX",
                    ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-18)),
                    Service = "Brake inspection",
                    Mileage = 84_101,
                    Technician = "Demo Tech",
                    Status = "Completed",
                    Notes = "Pads and rotors inspected. No issues found.",
                    CreatedUtc = DateTimeOffset.UtcNow.AddDays(-18)
                },
                new ServiceHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    CustomerId = vehicle.CustomerId,
                    CustomerVehicleId = vehicle.Id,
                    VehicleName = "2016 Acura MDX",
                    ServiceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-45)),
                    Service = "Oil change",
                    Mileage = 83_600,
                    Technician = "Demo Tech",
                    Status = "Completed",
                    Notes = "Oil and filter replaced.",
                    CreatedUtc = DateTimeOffset.UtcNow.AddDays(-45)
                }
            ]
        };
    }
}
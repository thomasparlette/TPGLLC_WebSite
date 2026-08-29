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

    public VehicleDetailsService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
    }

    public async Task<VehicleDetailsViewModel> GetAsync(Guid vehicleId)
    {

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
            .Include(x => x.Parts)
            .Include(x => x.Jobs)
            .ThenInclude(x => x.Parts)
            .Include(x => x.Inspections)
            .Include(x => x.Updates)
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
}

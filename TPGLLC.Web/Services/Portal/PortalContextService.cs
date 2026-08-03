using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services.Customers;

namespace TPGLLC.Web.Services.Portal;

public sealed class PortalContextService : IPortalContextService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;

    public PortalContextService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
    }

    public Task<PortalContextViewModel> GetAsync(CancellationToken cancellationToken = default)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();
        return GetAsync(current.UserId, cancellationToken, current);
    }

    public async Task<PortalContextViewModel> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await GetAsync(userId, cancellationToken, null);
    }

    public async Task<CustomerVehicle?> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.CustomerVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
    }

    private async Task<PortalContextViewModel> GetAsync(
        string userId,
        CancellationToken cancellationToken,
        CurrentCustomer? currentOverride)
    {
        var current = currentOverride ?? _currentCustomerAccessor.GetCurrentCustomer();

        if (!current.IsAuthenticated || string.IsNullOrWhiteSpace(userId))
        {
            return new PortalContextViewModel
            {
                CurrentCustomer = current
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var profile = await db.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId, cancellationToken);

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == userId, cancellationToken);

        IReadOnlyList<CustomerVehicle> vehicles = Array.Empty<CustomerVehicle>();
        IReadOnlyList<ServiceHistoryEntry> serviceHistory = Array.Empty<ServiceHistoryEntry>();
        IReadOnlyList<AppointmentRequest> appointmentRequests = Array.Empty<AppointmentRequest>();

        if (customer is not null)
        {
            vehicles = await db.CustomerVehicles
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.CreatedUtc)
                .ToListAsync(cancellationToken);

            serviceHistory = await db.ServiceHistoryEntries
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .OrderByDescending(x => x.ServiceDate)
                .ToListAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(current.Email))
        {
            appointmentRequests = await db.AppointmentRequests
                .AsNoTracking()
                .Where(x => x.Email == current.Email)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToListAsync(cancellationToken);
        }

        return new PortalContextViewModel
        {
            CurrentCustomer = current,
            Profile = profile,
            Customer = customer,
            Vehicles = vehicles,
            ServiceHistoryEntries = serviceHistory,
            AppointmentRequests = appointmentRequests
        };
    }
}
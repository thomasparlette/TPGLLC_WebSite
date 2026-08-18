using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Services.Customers;

namespace TPGLLC.Web.Services.Portal;

public sealed class PortalContextService : IPortalContextService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public PortalContextService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _userManager = userManager;
    }

    public async Task<PortalContextViewModel> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var current = _currentCustomerAccessor.GetCurrentCustomer();

        if (!current.IsAuthenticated || string.IsNullOrWhiteSpace(current.UserId))
        {
            return new PortalContextViewModel
            {
                CurrentCustomer = current
            };
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(current.UserId);

        var profile = await db.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId, cancellationToken);

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId, cancellationToken);

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
                .Include(x => x.Parts)
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

        var displayName =
            user?.DisplayName?.Trim()
            ?? string.Join(" ", new[] { user?.FirstName, user?.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName =
                string.Join(" ", new[] { profile?.FirstName, profile?.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName =
                string.Join(" ", new[] { customer?.FirstName, customer?.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = string.IsNullOrWhiteSpace(current.Email) ? "Customer" : current.Email;
        }

        return new PortalContextViewModel
        {
            CurrentCustomer = new CurrentCustomer
            {
                IsAuthenticated = current.IsAuthenticated,
                UserId = current.UserId,
                Email = current.Email,
                DisplayName = displayName,
                IsCustomer = current.IsCustomer,
                IsAdministrator = current.IsAdministrator
            },
            Profile = profile,
            Customer = customer,
            Vehicles = vehicles,
            ServiceHistoryEntries = serviceHistory,
            AppointmentRequests = appointmentRequests
        };
    }

    public async Task<CustomerVehicle?> GetVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.CustomerVehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
    }
}

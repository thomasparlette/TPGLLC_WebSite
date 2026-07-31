using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services.Customers;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;



public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly IBuildEnvironmentService _buildEnvironmentService;

    public DashboardService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IBuildEnvironmentService buildEnvironmentService)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _buildEnvironmentService = buildEnvironmentService;
    }

    public async Task<DashboardViewModel> GetAsync()
    {
        if (_buildEnvironmentService.IsBuildEnvironment)
        {
            return _buildEnvironmentService.CreateDashboard();
        }

        var current = _currentCustomerAccessor.GetCurrentCustomer();
        if (!current.IsAuthenticated)
        {
            return new DashboardViewModel();
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.ChangeTracker.Clear();

        var profile = await db.CustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationUserId == current.UserId);

        var vehicles = new List<CustomerVehicle>();
        var history = new List<ServiceHistoryEntry>();

        if (customer is not null)
        {
            vehicles = await db.CustomerVehicles
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.CreatedUtc)
                .ToListAsync();

            history = await db.ServiceHistoryEntries
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .OrderByDescending(x => x.ServiceDate)
                .Take(10)
                .ToListAsync();
        }

        var requests = new List<AppointmentRequest>();
        if (!string.IsNullOrWhiteSpace(current.Email))
        {
            requests = await db.AppointmentRequests
                .AsNoTracking()
                .Where(x => x.Email == current.Email)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .Take(10)
                .ToListAsync();
        }

        var displayName = "Customer";
        var name = $"{profile?.FirstName} {profile?.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            displayName = name;
        }

        var model = new DashboardViewModel
        {
            DisplayName = displayName,
            Vehicles = vehicles,
            Requests = requests,
            History = history
        };

        model.Activity = BuildActivity(model.Vehicles, model.Requests, model.History);
        return model;
    }

    private static List<ActivityItem> BuildActivity(
        List<CustomerVehicle> vehicles,
        List<AppointmentRequest> requests,
        List<ServiceHistoryEntry> history)
    {
        var items = new List<ActivityItem>();

        if (vehicles.Count > 0)
        {
            var newestVehicle = vehicles.First();
            items.Add(new ActivityItem(
                "🚗",
                "Vehicle added",
                $"{newestVehicle.ModelYear} {newestVehicle.Make} {newestVehicle.Model}".Trim() ?? string.Empty,
                newestVehicle.CreatedUtc.ToLocalTime().ToString("MMM d, yyyy")));
        }

        if (requests.Count > 0)
        {
            var latestRequest = requests.First();
            items.Add(new ActivityItem(
                "📅",
                "Appointment request",
                latestRequest.ServiceNeeded,
                latestRequest.SubmittedAtUtc.ToLocalTime().ToString("MMM d, yyyy")));
        }

        if (history.Count > 0)
        {
            var latestService = history.First();
            items.Add(new ActivityItem(
                "🛠️",
                "Service completed",
                $"{latestService.VehicleName} · {latestService.Service}",
                latestService.ServiceDate.ToString("MMM d, yyyy")));
        }

        return items;
    }
}
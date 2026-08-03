using Microsoft.EntityFrameworkCore;
using TPGLLC.Data.Entities;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public sealed class DashboardService : IDashboardService
{
    private readonly IPortalContextService _portalContextService;

    public DashboardService(IPortalContextService portalContextService)
    {
        _portalContextService = portalContextService;
    }

    public async Task<DashboardViewModel> GetAsync()
    {
        var portal = await _portalContextService.GetAsync();

        if (!portal.CurrentCustomer.IsAuthenticated)
        {
            return new DashboardViewModel();
        }

        var vehicles = portal.Vehicles.ToList();
        var history = portal.ServiceHistoryEntries.Take(10).ToList();
        var requests = portal.AppointmentRequests.Take(10).ToList();

        var model = new DashboardViewModel
        {
            DisplayName = string.IsNullOrWhiteSpace(portal.DisplayName)
                ? "Customer"
                : portal.DisplayName,
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
                $"{newestVehicle.ModelYear} {newestVehicle.Make} {newestVehicle.Model}".Trim(),
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
using TPGLLC.Data.Entities;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class DashboardViewModel
{
    public string DisplayName { get; set; } = "Customer";

    public List<CustomerVehicle> Vehicles { get; set; } = [];
    public List<AppointmentRequest> Requests { get; set; } = [];
    public List<ServiceHistoryEntry> History { get; set; } = [];
    public List<ActivityItem> Activity { get; set; } = [];

    public CustomerVehicle? PrimaryVehicle
        => Vehicles.FirstOrDefault(x => x.IsPrimary) ?? Vehicles.FirstOrDefault();

    public AppointmentRequest? NextRequest
        => Requests.FirstOrDefault();
}
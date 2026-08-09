using TPGLLC.Web.Components.PortalShared.Appointments;
using TPGLLC.Web.Components.PortalShared.Vehicles;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public interface IBuildEnvironmentService
{
    bool IsBuildEnvironment { get; }

    DashboardViewModel CreateDashboard();

    VehiclePageViewModel CreateVehicles(Guid? editingVehicleId = null);

    VehicleDetailsViewModel CreateVehicleDetails(Guid vehicleId);

    AppointmentPageViewModel CreateAppointments();

    WorkOrderPageViewModel CreateWorkOrders();
}

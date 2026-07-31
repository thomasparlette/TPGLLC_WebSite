using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public interface IBuildEnvironmentService
{
    bool IsBuildEnvironment { get; }

    DashboardViewModel CreateDashboard();

    VehiclePageViewModel CreateVehicles();

    AppointmentPageViewModel CreateAppointments();
}
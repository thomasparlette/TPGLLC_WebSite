using TPGLLC.Web.Components.PortalShared.Appointments;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Appointments;

public interface IAppointmentPortalService
{
    Task<AppointmentPageViewModel> GetAsync();
    Task<AppointmentPageViewModel> ResetAsync();
    Task<AppointmentPageViewModel> YearChangedAsync(AppointmentPageViewModel model);
    Task<AppointmentPageViewModel> MakeChangedAsync(AppointmentPageViewModel model);
    Task<AppointmentPageViewModel> SaveAsync(AppointmentPageViewModel model);

    Task RescheduleAsync(
        Guid requestId,
        AppointmentRescheduleFormModel form,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}

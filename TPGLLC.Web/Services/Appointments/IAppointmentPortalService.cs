using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Appointments;

public interface IAppointmentPortalService
{
    Task<AppointmentPageViewModel> GetAsync();
    Task<AppointmentPageViewModel> ResetAsync();
    Task<AppointmentPageViewModel> YearChangedAsync(AppointmentPageViewModel model);
    Task<AppointmentPageViewModel> MakeChangedAsync(AppointmentPageViewModel model);
    Task<AppointmentPageViewModel> SaveAsync(AppointmentPageViewModel model);
}
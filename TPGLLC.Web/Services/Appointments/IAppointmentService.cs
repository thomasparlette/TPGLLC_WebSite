using TPGLLC.Data.Entities;
using TPGLLC.Web.Services; // for AppointmentRequest

namespace TPGLLC.Web.Services.Appointments;

public interface IAppointmentService
{
    Task<AppointmentSubmissionResult> SubmitAsync(
        AppointmentRequest request,
        CancellationToken cancellationToken = default);
}
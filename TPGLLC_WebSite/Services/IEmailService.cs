using TPGLLC_WebSite.Models;

namespace TPGLLC_WebSite.Services;

public interface IEmailService
{
    Task SendPendingAppointmentAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
using TPGLLC_WebSite.Models;

namespace TPGLLC_WebSite.Services;

public interface IAppointmentRequestService
{
    Task<Guid> SubmitAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
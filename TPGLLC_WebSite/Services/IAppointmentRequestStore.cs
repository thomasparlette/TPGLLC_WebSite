using TPGLLC_WebSite.Models;

namespace TPGLLC_WebSite.Services;

public interface IAppointmentRequestStore
{
    Task SaveAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
using TPGLLC.Shared.Models;

namespace TPGLLC.Services;

public interface IAppointmentRequestService
{
    Task<Guid> SubmitAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
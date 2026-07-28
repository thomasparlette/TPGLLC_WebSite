using TPGLLC.Data.Entities;

namespace TPGLLC.Services.Scheduling;

public interface IAppointmentRequestService
{
    Task<Guid> SubmitAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
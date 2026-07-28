using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Stores;

public interface IAppointmentRequestStore
{
    Task SaveAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
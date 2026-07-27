using TPGLLC.Shared.Models;

namespace TPGLLC.Services;

public interface IEmailService
{
    Task SendPendingAppointmentAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
    Task SendCustomerConfirmationAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
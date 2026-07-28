using TPGLLC.Data.Entities;

namespace TPGLLC.Services.Messaging;

public interface IEmailService
{
    Task SendPendingAppointmentAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
    Task SendCustomerConfirmationAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
}
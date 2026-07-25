using TPGLLC_WebSite.Models;

namespace TPGLLC_WebSite.Services;

public sealed class AppointmentRequestService : IAppointmentRequestService
{
    private readonly IAppointmentRequestStore _store;
    private readonly IEmailService _email;

    public AppointmentRequestService(IAppointmentRequestStore store, IEmailService email)
    {
        _store = store;
        _email = email;
    }

    public async Task<Guid> SubmitAsync(AppointmentRequest request, CancellationToken cancellationToken = default)
    {
        request.RequestId = Guid.NewGuid();
        request.SubmittedAtUtc = DateTimeOffset.UtcNow;
        request.Status = "Pending";

        await _store.SaveAsync(request, cancellationToken);
        await _email.SendPendingAppointmentAsync(request, cancellationToken);

        return request.RequestId;
    }
}
namespace TPGLLC.Application.Appointments;

public sealed record CreateAppointmentResponse(
    Guid RequestId,
    string Status,
    DateTimeOffset SubmittedAtUtc);
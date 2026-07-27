namespace TPGLLC.Application.Contracts.V1;

public sealed record CreateAppointmentResponse(
    Guid RequestId,
    string Status,
    DateTimeOffset SubmittedAtUtc);
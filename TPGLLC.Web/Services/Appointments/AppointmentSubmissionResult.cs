namespace TPGLLC.Web.Services.Appointments;

public sealed record AppointmentSubmissionResult(bool Success, Guid RequestId, string? ErrorMessage)
{
    public static AppointmentSubmissionResult Ok(Guid requestId) => new(true, requestId, null);
    public static AppointmentSubmissionResult Fail(string message) => new(false, Guid.Empty, message);
}
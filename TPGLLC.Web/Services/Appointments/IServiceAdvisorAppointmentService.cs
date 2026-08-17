using TPGLLC.Data.Entities;

namespace TPGLLC.Web.Services.Appointments;

public interface IServiceAdvisorAppointmentService
{
    Task<List<AppointmentRequest>> GetOpenRequestsAsync(string? statusFilter = null, CancellationToken cancellationToken = default);
    Task<List<AppointmentRequest>> GetCalendarAppointmentsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<AppointmentActionResult> AcceptAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<AppointmentActionResult> ProposeChangeAsync(Guid requestId, string date, string time, string? message, CancellationToken cancellationToken = default);
    Task<AppointmentRequest?> GetByResponseTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<AppointmentActionResult> AcceptProposedChangeAsync(string token, CancellationToken cancellationToken = default);
    Task<AppointmentActionResult> RequestDifferentTimeAsync(string token, string date, string time, string? message, CancellationToken cancellationToken = default);
}

public sealed record AppointmentActionResult(bool Succeeded, string Message)
{
    public static AppointmentActionResult Ok(string message) => new(true, message);
    public static AppointmentActionResult Fail(string message) => new(false, message);
}

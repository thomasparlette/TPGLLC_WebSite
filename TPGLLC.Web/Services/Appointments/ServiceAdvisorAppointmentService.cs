using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using TPGLLC.Data;
using TPGLLC.Data.Entities;

namespace TPGLLC.Web.Services.Appointments;

public sealed class ServiceAdvisorAppointmentService : IServiceAdvisorAppointmentService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly IEmailSender _emailSender;
    private readonly AppointmentEmailOptions _options;
    private readonly ILogger<ServiceAdvisorAppointmentService> _logger;

    public ServiceAdvisorAppointmentService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        IEmailSender emailSender,
        IOptions<AppointmentEmailOptions> options,
        ILogger<ServiceAdvisorAppointmentService> logger)
    {
        _dbFactory = dbFactory;
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<AppointmentRequest>> GetOpenRequestsAsync(
        string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.AppointmentRequests
            .AsNoTracking()
            .Where(x =>
                x.Status == "Approved" ||
                x.Status == "Confirmed" ||
                x.Status == "New" ||
                x.Status == "Requested" ||
                x.Status == "Pending" ||
                x.Status == "CustomerRequestedChange" ||
                x.Status == "AwaitingCustomerApproval" ||
                x.Status == "RescheduleProposed" ||
                x.Status == "Cancelled");

        var normalizedFilter = string.IsNullOrWhiteSpace(statusFilter)
            ? "All"
            : statusFilter.Trim();

        query = normalizedFilter.ToUpperInvariant() switch
        {
            "APPROVED" => query.Where(x => x.Status == "Approved" || x.Status == "Confirmed"),
            "NEW" => query.Where(x => x.Status == "New" || x.Status == "Requested" || x.Status == "CustomerRequestedChange"),
            "PENDING" => query.Where(x => x.Status == "Pending" || x.Status == "AwaitingCustomerApproval" || x.Status == "RescheduleProposed"),
            "CANCELED" or "CANCELLED" => query.Where(x => x.Status == "Cancelled"),
            _ => query
        };

        return await query
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppointmentActionResult> AcceptAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.AppointmentRequests.FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (request is null) return AppointmentActionResult.Fail("Appointment request was not found.");
        if (IsClosed(request.Status)) return AppointmentActionResult.Fail("This request is already closed.");

        request.Status = "Confirmed";
        request.ProposedDate = null;
        request.ProposedTime = null;
        request.AdvisorMessage = null;
        request.ResponseToken = null;
        request.ResponseTokenExpiresUtc = null;
        await db.SaveChangesAsync(cancellationToken);

        var date = WebUtility.HtmlEncode(request.PreferredDate ?? string.Empty);
        var time = WebUtility.HtmlEncode(request.PreferredTime ?? string.Empty);
        var body = BuildEmail(request, "Your appointment is confirmed",
            $"<p>Your requested appointment has been accepted for <strong>{date}</strong> at <strong>{time}</strong>.</p>" +
            "<p>We look forward to seeing you. If you need to make a change, please contact the shop.</p>");

        await SendCustomerEmailAsync(request, $"{_options.ShopName} - Appointment Confirmed", body);
        return AppointmentActionResult.Ok("Appointment accepted and confirmation email sent.");
    }

    public async Task<AppointmentActionResult> ProposeChangeAsync(Guid requestId, string date, string time, string? message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(time))
            return AppointmentActionResult.Fail("A proposed date and time are required.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var request = await db.AppointmentRequests.FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (request is null) return AppointmentActionResult.Fail("Appointment request was not found.");
        if (IsClosed(request.Status)) return AppointmentActionResult.Fail("This request is already closed.");

        request.ProposedDate = date.Trim();
        request.ProposedTime = time.Trim();
        request.AdvisorMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        request.Status = "AwaitingCustomerApproval";
        request.ResponseToken = CreateToken();
        request.ResponseTokenExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);
        await db.SaveChangesAsync(cancellationToken);

        var link = BuildResponseUrl(request.ResponseToken);
        var note = string.IsNullOrWhiteSpace(request.AdvisorMessage)
            ? string.Empty
            : $"<p><strong>Service advisor note:</strong> {WebUtility.HtmlEncode(request.AdvisorMessage)}</p>";
        var body = BuildEmail(request, "Please review a proposed appointment time",
            $"<p>We would like to change your appointment to <strong>{WebUtility.HtmlEncode(request.ProposedDate)}</strong> at <strong>{WebUtility.HtmlEncode(request.ProposedTime)}</strong>.</p>" +
            note +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\" style=\"display:inline-block;padding:12px 18px;background:#0b2f67;color:#fff;text-decoration:none;border-radius:6px\">Review appointment</a></p>" +
            "<p>From that page you can accept the proposed time or request a different date/time.</p>");

        await SendCustomerEmailAsync(request, $"{_options.ShopName} - Appointment Time Approval Needed", body);
        return AppointmentActionResult.Ok("Proposed change sent to the customer for approval.");
    }

    public async Task<AppointmentRequest?> GetByResponseTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.AppointmentRequests.AsNoTracking().FirstOrDefaultAsync(
            x => x.ResponseToken == token && x.ResponseTokenExpiresUtc > DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task<AppointmentActionResult> AcceptProposedChangeAsync(string token, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var request = await FindValidTokenAsync(db, token, cancellationToken);
        if (request is null) return AppointmentActionResult.Fail("This appointment response link is invalid or has expired.");
        if (string.IsNullOrWhiteSpace(request.ProposedDate) || string.IsNullOrWhiteSpace(request.ProposedTime))
            return AppointmentActionResult.Fail("There is no proposed appointment time to accept.");

        request.PreferredDate = request.ProposedDate;
        request.PreferredTime = request.ProposedTime;
        request.ProposedDate = null;
        request.ProposedTime = null;
        request.Status = "Confirmed";
        request.ResponseToken = null;
        request.ResponseTokenExpiresUtc = null;
        await db.SaveChangesAsync(cancellationToken);

        var body = BuildEmail(request, "Your appointment is confirmed",
            $"<p>Thank you. Your appointment is confirmed for <strong>{WebUtility.HtmlEncode(request.PreferredDate)}</strong> at <strong>{WebUtility.HtmlEncode(request.PreferredTime)}</strong>.</p>");
        await SendCustomerEmailAsync(request, $"{_options.ShopName} - Appointment Confirmed", body);
        return AppointmentActionResult.Ok("Thank you. Your appointment has been confirmed.");
    }

    public async Task<AppointmentActionResult> RequestDifferentTimeAsync(string token, string date, string time, string? message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(time))
            return AppointmentActionResult.Fail("Please enter the date and time you would prefer.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var request = await FindValidTokenAsync(db, token, cancellationToken);
        if (request is null) return AppointmentActionResult.Fail("This appointment response link is invalid or has expired.");

        request.PreferredDate = date.Trim();
        request.PreferredTime = time.Trim();
        request.Message = string.IsNullOrWhiteSpace(message) ? request.Message : message.Trim();
        request.ProposedDate = null;
        request.ProposedTime = null;
        request.AdvisorMessage = null;
        request.Status = "CustomerRequestedChange";
        request.ResponseToken = null;
        request.ResponseTokenExpiresUtc = null;
        request.SubmittedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var body = BuildEmail(request, "We received your requested appointment change",
            $"<p>We received your request for <strong>{WebUtility.HtmlEncode(request.PreferredDate)}</strong> at <strong>{WebUtility.HtmlEncode(request.PreferredTime)}</strong>.</p>" +
            "<p>A service advisor will review the new request and confirm it with you.</p>");
        await SendCustomerEmailAsync(request, $"{_options.ShopName} - Appointment Change Received", body);
        return AppointmentActionResult.Ok("Your requested date/time was sent to the service advisor.");
    }

    private static async Task<AppointmentRequest?> FindValidTokenAsync(TPGLLCDbContext db, string token, CancellationToken cancellationToken) =>
        await db.AppointmentRequests.FirstOrDefaultAsync(
            x => x.ResponseToken == token && x.ResponseTokenExpiresUtc > DateTimeOffset.UtcNow, cancellationToken);

    private async Task SendCustomerEmailAsync(AppointmentRequest request, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return;
        try { await _emailSender.SendEmailAsync(request.Email, subject, body); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appointment {RequestId} was updated but customer email could not be sent.", request.RequestId);
            throw new InvalidOperationException("The appointment was updated, but the confirmation email could not be sent. Check SMTP settings and resend as needed.", ex);
        }
    }

    private string BuildEmail(AppointmentRequest request, string heading, string content) => $"""
        <div style="font-family:Arial,sans-serif;max-width:640px;margin:auto;color:#17233c">
          <h2>{WebUtility.HtmlEncode(_options.ShopName)}</h2>
          <h3>{WebUtility.HtmlEncode(heading)}</h3>
          <p>Hello {WebUtility.HtmlEncode(request.Name ?? "Customer")},</p>
          {content}
          <hr style="border:0;border-top:1px solid #ddd;margin:24px 0" />
          <p><strong>Service:</strong> {WebUtility.HtmlEncode(request.ServiceNeeded ?? string.Empty)}<br />
          <strong>Vehicle:</strong> {WebUtility.HtmlEncode(BuildVehicle(request))}</p>
          <p>{WebUtility.HtmlEncode(_options.ShopPhone)} · {WebUtility.HtmlEncode(_options.ShopEmail)}</p>
        </div>
        """;

    private string BuildResponseUrl(string token)
    {
        var root = string.IsNullOrWhiteSpace(_options.WebsiteUrl) ? "https://tomparlettegarage.org/" : _options.WebsiteUrl.Trim();
        if (!root.EndsWith('/')) root += "/";
        return $"{root}appointment/respond/{Uri.EscapeDataString(token)}";
    }

    private static string BuildVehicle(AppointmentRequest request) =>
        string.Join(" ", new[] { request.VehicleYear, request.VehicleMake, request.VehicleModel }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string CreateToken() => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    private static bool IsClosed(string? status) =>
        status is not null && (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || status.Equals("Declined", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("Closed", StringComparison.OrdinalIgnoreCase));
}

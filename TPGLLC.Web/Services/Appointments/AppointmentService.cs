using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services;

namespace TPGLLC.Web.Services.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private readonly AppointmentEmailOptions _options;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IOptions<AppointmentEmailOptions> options,
        ILogger<AppointmentService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AppointmentSubmissionResult> SubmitAsync(
        AppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateOptions();
            ValidateRequest(request);

            var requestId = Guid.NewGuid();

            var vehicleSummary = string.Join(" ", new[]
            {
                request.VehicleYear,
                request.VehicleMake,
                request.VehicleModel
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var subject = $"New Appointment Request - {request.Name} - {vehicleSummary}";
            var body = BuildHtmlBody(request, requestId);

            await SendEmailAsync(
                toAddress: _options.ToAddress,
                subject: subject,
                htmlBody: body,
                replyTo: request.Email,
                replyToName: request.Name,
                cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var customerSubject = "We received your appointment request";
                var customerBody = BuildCustomerConfirmationBody(request, requestId);

                await SendEmailAsync(
                    toAddress: request.Email!,
                    subject: customerSubject,
                    htmlBody: customerBody,
                    replyTo: _options.FromAddress,
                    replyToName: _options.FromName,
                    cancellationToken: cancellationToken);
            }

            return AppointmentSubmissionResult.Ok(requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit appointment request.");
            return AppointmentSubmissionResult.Fail("We were unable to send your appointment request right now.");
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("Gmail:Host is missing.");

        if (_options.Port <= 0)
            throw new InvalidOperationException("Gmail:Port is missing or invalid.");

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
            throw new InvalidOperationException("Gmail:FromAddress is missing.");

        if (string.IsNullOrWhiteSpace(_options.ToAddress))
            throw new InvalidOperationException("Gmail:ToAddress is missing.");
    }

    private static void ValidateRequest(AppointmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new InvalidOperationException("Phone is required.");

        if (string.IsNullOrWhiteSpace(request.VehicleYear))
            throw new InvalidOperationException("Vehicle year is required.");

        if (string.IsNullOrWhiteSpace(request.VehicleMake))
            throw new InvalidOperationException("Vehicle make is required.");

        if (string.IsNullOrWhiteSpace(request.VehicleModel))
            throw new InvalidOperationException("Vehicle model is required.");

        if (string.IsNullOrWhiteSpace(request.PreferredDate))
            throw new InvalidOperationException("Preferred date is required.");

        if (string.IsNullOrWhiteSpace(request.PreferredTime))
            throw new InvalidOperationException("Preferred time is required.");

        if (string.IsNullOrWhiteSpace(request.ServiceNeeded))
            throw new InvalidOperationException("Service needed is required.");

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new InvalidOperationException("Message is required.");
    }

    private async Task SendEmailAsync(
        string toAddress,
        string subject,
        string htmlBody,
        string? replyTo,
        string? replyToName,
        CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(toAddress));

        if (!string.IsNullOrWhiteSpace(replyTo))
        {
            message.ReplyToList.Add(new MailAddress(replyTo, string.IsNullOrWhiteSpace(replyToName) ? replyTo : replyToName));
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static string BuildHtmlBody(AppointmentRequest request, Guid requestId)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var vehicleSummary = string.Join(" ", new[]
        {
            request.VehicleYear,
            request.VehicleMake,
            request.VehicleModel
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var sb = new StringBuilder();
        sb.AppendLine("<html><body style='font-family:Segoe UI,Arial,sans-serif;'>");
        sb.AppendLine("<h2>New Appointment Request</h2>");
        sb.AppendLine("<table cellpadding='6' cellspacing='0' style='border-collapse:collapse;'>");
        AppendRow(sb, "Request ID", requestId.ToString("D"));
        AppendRow(sb, "Name", E(request.Name));
        AppendRow(sb, "Phone", E(request.Phone));
        AppendRow(sb, "Email", E(request.Email));
        AppendRow(sb, "Vehicle", E(vehicleSummary));
        AppendRow(sb, "VIN", E(request.Vin));
        AppendRow(sb, "Mileage", E(request.Mileage));
        AppendRow(sb, "Preferred Date", E(request.PreferredDate));
        AppendRow(sb, "Preferred Time", E(request.PreferredTime));
        AppendRow(sb, "Service Needed", E(request.ServiceNeeded));
        AppendRow(sb, "Company", E(request.Company));
        sb.AppendLine("</table>");
        sb.AppendLine("<h3>Message</h3>");
        sb.AppendLine($"<p>{E(request.Message)}</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string BuildCustomerConfirmationBody(AppointmentRequest request, Guid requestId)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var sb = new StringBuilder();
        sb.AppendLine("<html><body style='font-family:Segoe UI,Arial,sans-serif;'>");
        sb.AppendLine("<h2>Appointment Request Received</h2>");
        sb.AppendLine("<p>Thank you for contacting Tom Parlette Garage LLC. Your request has been received and is pending review.</p>");
        sb.AppendLine($"<p><strong>Reference:</strong> {requestId:D}</p>");
        sb.AppendLine("<p><strong>Vehicle:</strong> " + E(string.Join(" ", new[]
        {
            request.VehicleYear,
            request.VehicleMake,
            request.VehicleModel
        }.Where(x => !string.IsNullOrWhiteSpace(x)))) + "</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string label, string value)
    {
        sb.AppendLine("<tr>");
        sb.AppendLine($"<td style='font-weight:bold; border:1px solid #ccc;'>{WebUtility.HtmlEncode(label)}</td>");
        sb.AppendLine($"<td style='border:1px solid #ccc;'>{WebUtility.HtmlEncode(value)}</td>");
        sb.AppendLine("</tr>");
    }
}
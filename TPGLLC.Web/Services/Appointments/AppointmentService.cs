using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services;

namespace TPGLLC.Web.Services.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private readonly AppointmentEmailOptions _options;
    private readonly IEmailTemplateRenderer _templates;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IOptions<AppointmentEmailOptions> options,
        IEmailTemplateRenderer templates,
        ILogger<AppointmentService> logger)
    {
        _options = options.Value;
        _templates = templates;
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
            var year = DateTime.UtcNow.Year;

            var vehicleSummary = string.Join(" ", new[]
            {
                request.VehicleYear,
                request.VehicleMake,
                request.VehicleModel
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var tokens = new Dictionary<string, string?>
            {
                ["ReferenceNumber"] = requestId.ToString("D"),
                ["CustomerName"] = request.Name,
                ["CustomerPhone"] = request.Phone,
                ["CustomerEmail"] = request.Email,
                ["VehicleSummary"] = vehicleSummary,
                ["Vin"] = string.IsNullOrWhiteSpace(request.Vin) ? "Not provided" : request.Vin,
                ["Mileage"] = string.IsNullOrWhiteSpace(request.Mileage) ? "Not provided" : request.Mileage,
                ["PreferredDate"] = request.PreferredDate,
                ["PreferredTime"] = request.PreferredTime,
                ["ServiceNeeded"] = request.ServiceNeeded,
                ["Message"] = request.Message,
                ["ShopName"] = _options.ShopName,
                ["Tagline"] = _options.Tagline,
                ["WebsiteUrl"] = _options.WebsiteUrl,
                ["LogoUrl"] = _options.LogoUrl,
                ["ShopPhone"] = _options.ShopPhone,
                ["ShopEmail"] = _options.ShopEmail,
                ["Year"] = year.ToString()
            };

            var internalSubject = $"New Appointment Request - {request.Name} - {vehicleSummary}";
            var internalBody = _templates.Render("InternalAppointment.html", tokens);

            await SendEmailAsync(
                _options.ToAddress,
                internalSubject,
                internalBody,
                request.Email,
                request.Name,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var customerSubject = $"{_options.ShopName} - Appointment Request Received";
                var customerBody = _templates.Render("CustomerAppointment.html", tokens);

                await SendEmailAsync(
                    request.Email!,
                    customerSubject,
                    customerBody,
                    _options.FromAddress,
                    _options.FromName,
                    cancellationToken);
            }

            return AppointmentSubmissionResult.Ok(requestId);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Appointment email template missing.");
            return AppointmentSubmissionResult.Fail("Email template missing. Check the EmailTemplates Folder");
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP failure while sending appointment email.");
            return AppointmentSubmissionResult.Fail("SMTP login or Gmail app password is not correct.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Appointment validation/configuration failure.");
            return AppointmentSubmissionResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit appointment request.");
            return AppointmentSubmissionResult.Fail("We were unable to send your appointment request right now.");
        }
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
    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("Gmail:Host is missing.");

        if (_options.Port <= 0)
            throw new InvalidOperationException("Gmail:Port is missing or invalid.");
        
        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("Gmail:Username is missing.");
        
        if (string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("Gmail:Password is missing.");

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
}
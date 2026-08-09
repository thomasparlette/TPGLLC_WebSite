using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Web.Services;
using TPGLLC.Web.Services.Customers;

namespace TPGLLC.Web.Services.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly AppointmentEmailOptions _options;
    private readonly IEmailTemplateRenderer _templates;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        IOptions<AppointmentEmailOptions> options,
        IEmailTemplateRenderer templates,
        ILogger<AppointmentService> logger)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
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
            ValidateRequest(request);

            var current = _currentCustomerAccessor.GetCurrentCustomer();
            var requestId = Guid.NewGuid();
            var submittedAtUtc = DateTimeOffset.UtcNow;

            await SaveRequestAsync(request, requestId, current, submittedAtUtc, cancellationToken);

            if (!CanSendEmails())
            {
                _logger.LogInformation(
                    "Appointment request {RequestId} saved without email because SMTP settings are incomplete.",
                    requestId);

                return AppointmentSubmissionResult.Ok(requestId);
            }

            ValidateOptions();

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
            return AppointmentSubmissionResult.Fail("Email template missing. Check the EmailTemplates folder.");
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

    private async Task SaveRequestAsync(
        AppointmentRequest request,
        Guid requestId,
        CurrentCustomer current,
        DateTimeOffset submittedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = new AppointmentRequest
        {
            RequestId = requestId,
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email)
                ? current.Email?.Trim()
                : request.Email.Trim(),
            VehicleYear = string.IsNullOrWhiteSpace(request.VehicleYear) ? null : request.VehicleYear.Trim(),
            VehicleMake = string.IsNullOrWhiteSpace(request.VehicleMake) ? null : request.VehicleMake.Trim(),
            VehicleModel = string.IsNullOrWhiteSpace(request.VehicleModel) ? null : request.VehicleModel.Trim(),
            Vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim(),
            Mileage = string.IsNullOrWhiteSpace(request.Mileage) ? null : request.Mileage.Trim(),
            PreferredDate = request.PreferredDate.Trim(),
            PreferredTime = request.PreferredTime.Trim(),
            ServiceNeeded = request.ServiceNeeded.Trim(),
            Message = request.Message.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Requested" : request.Status.Trim(),
            SubmittedAtUtc = submittedAtUtc
        };

        db.AppointmentRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
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
            message.ReplyToList.Add(
                new MailAddress(replyTo, string.IsNullOrWhiteSpace(replyToName) ? replyTo : replyToName));
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private bool CanSendEmails()
    {
        return
            !string.IsNullOrWhiteSpace(_options.Host) &&
            _options.Port > 0 &&
            !string.IsNullOrWhiteSpace(_options.Username) &&
            !string.IsNullOrWhiteSpace(_options.Password) &&
            !string.IsNullOrWhiteSpace(_options.FromAddress) &&
            !string.IsNullOrWhiteSpace(_options.ToAddress);
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
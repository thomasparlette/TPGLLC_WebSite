using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TPGLLC.Data;
using TPGLLC.Data.Entities;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Services;
using TPGLLC.Web.Services.Customers;

namespace TPGLLC.Web.Services.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(5);

    private readonly IDbContextFactory<TPGLLCDbContext> _dbFactory;
    private readonly ICurrentCustomerAccessor _currentCustomerAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppointmentEmailOptions _options;
    private readonly IEmailTemplateRenderer _templates;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IDbContextFactory<TPGLLCDbContext> dbFactory,
        ICurrentCustomerAccessor currentCustomerAccessor,
        UserManager<ApplicationUser> userManager,
        IOptions<AppointmentEmailOptions> options,
        IEmailTemplateRenderer templates,
        ILogger<AppointmentService> logger)
    {
        _dbFactory = dbFactory;
        _currentCustomerAccessor = currentCustomerAccessor;
        _userManager = userManager;
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
            NormalizeRequest(request, current);

            var submittedAtUtc = DateTimeOffset.UtcNow;
            var saved = await SaveRequestAsync(request, current, submittedAtUtc, cancellationToken);

            if (!saved.IsNew)
            {
                _logger.LogInformation(
                    "Ignored duplicate appointment submission for existing request {RequestId}.",
                    saved.RequestId);

                return AppointmentSubmissionResult.Ok(saved.RequestId);
            }

            var requestId = saved.RequestId;
            var accountSetupLink = await BuildAccountSetupLinkAsync(saved.User);

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
                ["Year"] = year.ToString(CultureInfo.InvariantCulture)
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

            var customerSubject = $"{_options.ShopName} - Appointment Request Received";
            var customerBody = _templates.Render("CustomerAppointment.html", tokens)
                .Replace(
                    "{{AccountSetupSection}}",
                    BuildAccountSetupSection(accountSetupLink),
                    StringComparison.OrdinalIgnoreCase);

            await SendEmailAsync(
                request.Email!,
                customerSubject,
                customerBody,
                _options.FromAddress,
                _options.FromName,
                cancellationToken);

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

    private async Task<AppointmentSaveResult> SaveRequestAsync(
        AppointmentRequest request,
        CurrentCustomer current,
        DateTimeOffset submittedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await EnsureAccountAsync(request, current.UserId);
        await EnsureCustomerAndVehicleAsync(db, request, user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var duplicateSince = submittedAtUtc.Subtract(DuplicateWindow);
        var duplicate = await db.AppointmentRequests
            .AsNoTracking()
            .Where(x =>
                x.SubmittedAtUtc >= duplicateSince &&
                x.Email == request.Email &&
                x.Name == request.Name &&
                x.Phone == request.Phone &&
                x.VehicleYear == request.VehicleYear &&
                x.VehicleMake == request.VehicleMake &&
                x.VehicleModel == request.VehicleModel &&
                x.Vin == request.Vin &&
                x.Mileage == request.Mileage &&
                x.PreferredDate == request.PreferredDate &&
                x.PreferredTime == request.PreferredTime &&
                x.ServiceNeeded == request.ServiceNeeded &&
                x.Message == request.Message)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (duplicate is not null)
        {
            return new AppointmentSaveResult(duplicate.RequestId, false, user);
        }

        var entity = new AppointmentRequest
        {
            RequestId = Guid.NewGuid(),
            Name = request.Name!,
            Phone = request.Phone!,
            Email = request.Email!,
            VehicleYear = request.VehicleYear,
            VehicleMake = request.VehicleMake,
            VehicleModel = request.VehicleModel,
            Vin = request.Vin,
            Mileage = request.Mileage,
            PreferredDate = request.PreferredDate!,
            PreferredTime = request.PreferredTime!,
            ServiceNeeded = request.ServiceNeeded!,
            Message = request.Message!,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Requested" : request.Status.Trim(),
            SubmittedAtUtc = submittedAtUtc
        };

        db.AppointmentRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new AppointmentSaveResult(entity.RequestId, true, user);
    }

    private async Task<ApplicationUser> EnsureAccountAsync(
        AppointmentRequest request,
        string currentUserId)
    {
        ApplicationUser? user = null;

        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            user = await _userManager.FindByIdAsync(currentUserId);
        }

        user ??= await _userManager.FindByEmailAsync(request.Email!);

        if (user is null)
        {
            var (firstName, lastName) = SplitName(request.Name);
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = false,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = request.Name!,
                IsActive = true,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to create the customer account: " +
                    string.Join(" ", createResult.Errors.Select(x => x.Description)));
            }

            // No role is assigned here. An administrator can assign one later.
            return user;
        }

        var (existingFirstName, existingLastName) = SplitName(request.Name);
        var changed = false;

        if (string.IsNullOrWhiteSpace(user.DisplayName))
        {
            user.DisplayName = request.Name!;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(user.FirstName))
        {
            user.FirstName = existingFirstName;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(user.LastName))
        {
            user.LastName = existingLastName;
            changed = true;
        }

        if (changed)
        {
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to update the customer account: " +
                    string.Join(" ", updateResult.Errors.Select(x => x.Description)));
            }
        }

        return user;
    }

    private static async Task EnsureCustomerAndVehicleAsync(
        TPGLLCDbContext db,
        AppointmentRequest request,
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var (firstName, lastName) = SplitName(request.Name);

        var customer = await db.Customers
            .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id, cancellationToken);

        customer ??= await db.Customers
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                ApplicationUserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = request.Email,
                Phone = request.Phone,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            db.Customers.Add(customer);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(customer.ApplicationUserId))
            {
                customer.ApplicationUserId = user.Id;
            }

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                customer.Email = request.Email;
            }

            if (string.IsNullOrWhiteSpace(customer.Phone))
            {
                customer.Phone = request.Phone;
            }

            customer.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        var profile = await db.CustomerProfiles
            .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id, cancellationToken);

        if (profile is null)
        {
            db.CustomerProfiles.Add(new CustomerProfile
            {
                ApplicationUserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                Phone = request.Phone,
                ReceiveEmail = true,
                CreatedUtc = DateTimeOffset.UtcNow
            });
        }
        else if (string.IsNullOrWhiteSpace(profile.Phone) && !string.IsNullOrWhiteSpace(request.Phone))
        {
            profile.Phone = request.Phone;
            profile.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        var modelYear = int.TryParse(
            request.VehicleYear,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsedYear)
                ? parsedYear
                : (int?)null;

        var mileage = TryParseMileage(request.Mileage, out var parsedMileage)
            ? parsedMileage
            : null;

        CustomerVehicle? vehicle = null;

        if (!string.IsNullOrWhiteSpace(request.Vin))
        {
            vehicle = await db.CustomerVehicles
                .FirstOrDefaultAsync(
                    x => x.CustomerId == customer.Id && x.Vin == request.Vin,
                    cancellationToken);
        }

        if (vehicle is null && modelYear.HasValue)
        {
            vehicle = await db.CustomerVehicles
                .FirstOrDefaultAsync(
                    x => x.CustomerId == customer.Id &&
                         x.ModelYear == modelYear &&
                         x.Make == request.VehicleMake &&
                         x.Model == request.VehicleModel,
                    cancellationToken);
        }

        if (vehicle is null)
        {
            vehicle = new CustomerVehicle
            {
                CustomerId = customer.Id,
                ModelYear = modelYear,
                Make = request.VehicleMake,
                Model = request.VehicleModel,
                Vin = request.Vin,
                Mileage = mileage,
                IsPrimary = !await db.CustomerVehicles
                    .AnyAsync(x => x.CustomerId == customer.Id && x.IsPrimary, cancellationToken),
                CreatedUtc = DateTimeOffset.UtcNow
            };

            db.CustomerVehicles.Add(vehicle);
        }
        else
        {
            if (mileage.HasValue)
            {
                vehicle.Mileage = mileage;
            }

            if (string.IsNullOrWhiteSpace(vehicle.Vin) && !string.IsNullOrWhiteSpace(request.Vin))
            {
                vehicle.Vin = request.Vin;
            }

            vehicle.UpdatedUtc = DateTimeOffset.UtcNow;
        }
    }

    private async Task<string?> BuildAccountSetupLinkAsync(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.PasswordHash) || string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var baseUrl = _options.WebsiteUrl.TrimEnd('/');

        return $"{baseUrl}/Identity/Account/ResetPassword?code={Uri.EscapeDataString(code)}&email={Uri.EscapeDataString(user.Email)}";
    }

    private static string BuildAccountSetupSection(string? accountSetupLink)
    {
        if (string.IsNullOrWhiteSpace(accountSetupLink))
        {
            return string.Empty;
        }

        var encodedLink = WebUtility.HtmlEncode(accountSetupLink);

        return "<div style=\"margin-top:28px;padding:16px 18px;border-left:4px solid #0f2f70;background:#f3f6fb;border-radius:10px;\">" +
               "<div style=\"font-weight:700;margin-bottom:6px;\">Finish setting up your account</div>" +
               "<div style=\"font-size:14px;line-height:1.6;\">Create a password to finish your account and access your appointment information.</div>" +
               $"<p style=\"margin:12px 0 0;\"><a href=\"{encodedLink}\" style=\"display:inline-block;padding:10px 16px;background:#0f2f70;color:#fff;text-decoration:none;border-radius:8px;font-weight:700;\">Create your password</a></p>" +
               "</div>";
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

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (!new EmailAddressAttribute().IsValid(request.Email.Trim()))
            throw new InvalidOperationException("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(request.VehicleYear))
            throw new InvalidOperationException("Vehicle year is required.");

        if (!int.TryParse(request.VehicleYear.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) ||
            year is < 1900 or > 3000)
            throw new InvalidOperationException("Vehicle year must be a valid year.");

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

    private static void NormalizeRequest(AppointmentRequest request, CurrentCustomer current)
    {
        request.Name = request.Name?.Trim();
        request.Phone = request.Phone?.Trim();
        request.Email = current.IsAuthenticated && !string.IsNullOrWhiteSpace(current.Email)
            ? current.Email.Trim()
            : request.Email?.Trim();
        request.VehicleYear = request.VehicleYear?.Trim();
        request.VehicleMake = request.VehicleMake?.Trim();
        request.VehicleModel = request.VehicleModel?.Trim();
        request.Vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim();
        request.Mileage = string.IsNullOrWhiteSpace(request.Mileage) ? null : request.Mileage.Trim();
        request.PreferredDate = request.PreferredDate?.Trim();
        request.PreferredTime = request.PreferredTime?.Trim();
        request.ServiceNeeded = request.ServiceNeeded?.Trim();
        request.Message = request.Message?.Trim();
    }

    private static (string FirstName, string LastName) SplitName(string? displayName)
    {
        var parts = (displayName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return ("Customer", string.Empty);
        }

        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        return (parts[0], string.Join(" ", parts.Skip(1)));
    }

    private static bool TryParseMileage(string? value, out int? mileage)
    {
        mileage = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Replace(",", string.Empty, StringComparison.Ordinal).Trim();
        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            mileage = parsed;
            return true;
        }

        return false;
    }

    private sealed record AppointmentSaveResult(
        Guid RequestId,
        bool IsNew,
        ApplicationUser User);
}

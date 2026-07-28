using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TPGLLC.Data.Entities;

namespace TPGLLC.Services.Messaging;

public sealed class SmtpEmailService : IEmailService
{
    private readonly GmailOptions _options;

    public SmtpEmailService(IOptions<GmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendPendingAppointmentAsync(AppointmentRequest request, CancellationToken cancellationToken = default)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = $"Pending Appointment Request #{request.RequestId:N} - {request.Name}",
            Body = BuildStaffHtmlBody(request),
            IsBodyHtml = true
        };

        mail.To.Add(_options.ToAddress);
        mail.ReplyToList.Add(new MailAddress(request.Email!, request.Name));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(mail, cancellationToken);
    }

    public async Task SendCustomerConfirmationAsync(AppointmentRequest request, CancellationToken cancellationToken = default)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = $"Appointment Request Received #{request.RequestId:N}",
            Body = BuildCustomerHtmlBody(request),
            IsBodyHtml = true
        };

        mail.To.Add(new MailAddress(request.Email!, request.Name));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(mail, cancellationToken);
    }

    private static string BuildStaffHtmlBody(AppointmentRequest request)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
        return $"""
        <html><body style="font-family:Arial,sans-serif;background:#f4f6f8;margin:0;padding:0;color:#12284c;">
          <div style="max-width:700px;margin:0 auto;padding:24px;">
            <div style="background:#0d2a63;color:#fff;border-radius:18px 18px 0 0;padding:18px 22px;">
              <div style="font-size:20px;font-weight:700;">Tom Parlette Garage LLC</div>
              <div style="font-size:13px;opacity:.9;">Built on Trust. Driven by Results.</div>
            </div>
            <div style="background:#fff;border:1px solid #e3e8f0;border-top:none;border-radius:0 0 18px 18px;padding:22px;">
              <h2 style="margin:0 0 12px;color:#0d2a63;">New Appointment Request</h2>
              <p><strong>Status:</strong> Pending review</p>
              <p><strong>Request ID:</strong> {E(request.RequestId.ToString())}</p>
              <p><strong>Submitted:</strong> {E(request.SubmittedAtUtc.ToString("u"))}</p>
              <hr style="border:none;border-top:1px solid #e3e8f0;margin:16px 0;" />
              <p><strong>Name:</strong> {E(request.Name)}</p>
              <p><strong>Phone:</strong> {E(request.Phone)}</p>
              <p><strong>Email:</strong> {E(request.Email)}</p>
              <p><strong>Vehicle Type:</strong> {E(request.VehicleType)}</p>
              <p><strong>VIN:</strong> {E(request.Vin)}</p>
              <p><strong>Vehicle:</strong> {E(request.VehicleYear)} {E(request.VehicleMake)} {E(request.VehicleModel)}</p>
              <p><strong>Mileage:</strong> {E(request.Mileage)}</p>
              <p><strong>Preferred Date:</strong> {E(request.PreferredDate)}</p>
              <p><strong>Preferred Time:</strong> {E(request.PreferredTime)}</p>
              <p><strong>Service Needed:</strong> {E(request.ServiceNeeded)}</p>
              <p><strong>Message:</strong></p>
              <div style="white-space:pre-wrap;background:#f8fafc;border:1px solid #e3e8f0;border-radius:12px;padding:12px;">{E(request.Message)}</div>
            </div>
          </div>
        </body></html>
        """;
    }

    private static string BuildCustomerHtmlBody(AppointmentRequest request)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
        return $"""
        <html><body style="font-family:Arial,sans-serif;background:#f4f6f8;margin:0;padding:0;color:#12284c;">
          <div style="max-width:700px;margin:0 auto;padding:24px;">
            <div style="background:#0d2a63;color:#fff;border-radius:18px 18px 0 0;padding:18px 22px;">
              <div style="font-size:20px;font-weight:700;">Tom Parlette Garage LLC</div>
              <div style="font-size:13px;opacity:.9;">Built on Trust. Driven by Results.</div>
            </div>
            <div style="background:#fff;border:1px solid #e3e8f0;border-top:none;border-radius:0 0 18px 18px;padding:22px;">
              <h2 style="margin:0 0 12px;color:#0d2a63;">We received your appointment request</h2>
              <p>Thank you for contacting Tom Parlette Garage LLC. Your request is now pending review.</p>
              <div style="background:#eef2f8;border-left:4px solid #d71920;border-radius:12px;padding:12px 14px;margin:0 0 16px;">
                <p style="margin:0 0 6px;"><strong>Request Number:</strong> {E(request.RequestId.ToString())}</p>
                <p style="margin:0;"><strong>Status:</strong> Pending review</p>
              </div>
              <p><strong>Name:</strong> {E(request.Name)}</p>
              <p><strong>Phone:</strong> {E(request.Phone)}</p>
              <p><strong>Email:</strong> {E(request.Email)}</p>
              <p><strong>Vehicle Type:</strong> {E(request.VehicleType)}</p>
              <p><strong>VIN:</strong> {E(request.Vin)}</p>
              <p><strong>Vehicle:</strong> {E(request.VehicleYear)} {E(request.VehicleMake)} {E(request.VehicleModel)}</p>
              <p><strong>Mileage:</strong> {E(request.Mileage)}</p>
              <p><strong>Preferred Date:</strong> {E(request.PreferredDate)}</p>
              <p><strong>Preferred Time:</strong> {E(request.PreferredTime)}</p>
              <p><strong>Service Needed:</strong> {E(request.ServiceNeeded)}</p>
              <p><strong>Message:</strong></p>
              <div style="white-space:pre-wrap;background:#f8fafc;border:1px solid #e3e8f0;border-radius:12px;padding:12px;">{E(request.Message)}</div>
              <p style="margin-top:16px;">We will review your request and contact you as soon as possible.</p>
            </div>
          </div>
        </body></html>
        """;
    }
}
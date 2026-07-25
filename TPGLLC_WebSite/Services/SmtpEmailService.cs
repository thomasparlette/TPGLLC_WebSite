using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TPGLLC_WebSite.Models;

namespace TPGLLC_WebSite.Services;

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
            Subject = $"Pending Appointment Request: {request.Name}",
            Body = BuildHtmlBody(request),
            IsBodyHtml = true
        };

        mail.To.Add(_options.ToAddress);
        mail.ReplyToList.Add(new MailAddress(request.Email!, request.Name));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(mail);
    }

    private static string BuildHtmlBody(AppointmentRequest request)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

        return $"""
                <h2>New Appointment Request</h2>
                <p><strong>Status:</strong> Pending review</p>
                <p><strong>Request ID:</strong> {E(request.RequestId.ToString())}</p>
                <p><strong>Submitted:</strong> {E(request.SubmittedAtUtc.ToString("u"))}</p>
                <hr />
                <p><strong>Name:</strong> {E(request.Name)}</p>
                <p><strong>Phone:</strong> {E(request.Phone)}</p>
                <p><strong>Email:</strong> {E(request.Email)}</p>
                <p><strong>Vehicle Type:</strong> {E(request.VehicleType)}</p>
                <p><strong>Vehicle:</strong> {E(request.VehicleYear)} {E(request.VehicleMake)} {E(request.VehicleModel)}</p>
                <p><strong>Mileage:</strong> {E(request.Mileage)}</p>
                <p><strong>Preferred Date:</strong> {E(request.PreferredDate)}</p>
                <p><strong>Preferred Time:</strong> {E(request.PreferredTime)}</p>
                <p><strong>Service Needed:</strong> {E(request.ServiceNeeded)}</p>
                <p><strong>Message:</strong></p>
                <pre style="white-space:pre-wrap;font-family:inherit">{E(request.Message)}</pre>
                """;
    }
}
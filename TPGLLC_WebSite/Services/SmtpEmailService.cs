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

    public async Task SendContactMessageAsync(ContactMessage message, CancellationToken cancellationToken = default)
    {
        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = $"Website message from {message.Name}",
            Body = BuildHtmlBody(message),
            IsBodyHtml = true
        };

        mail.To.Add(_options.ToAddress);
        mail.ReplyToList.Add(new MailAddress(message.Email!, message.Name));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        await client.SendMailAsync(mail, cancellationToken);
    }

    private static string BuildHtmlBody(ContactMessage message)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

        return $"""
                <h2>New Website Message</h2>
                <p><strong>Name:</strong> {E(message.Name)}</p>
                <p><strong>Phone:</strong> {E(message.Phone)}</p>
                <p><strong>Email:</strong> {E(message.Email)}</p>
                <p><strong>Message:</strong></p>
                <pre style="white-space:pre-wrap;font-family:inherit">{E(message.Body)}</pre>
                """;
    }
}
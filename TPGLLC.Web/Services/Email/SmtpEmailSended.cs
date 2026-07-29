using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace TPGLLC.Web.Services.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = Get("Smtp:Host", "Gmail:Host");
        var port = GetInt("Smtp:Port", "Gmail:Port", 587);
        var userName = Get("Smtp:UserName", "Gmail:UserName", "Gmail:Username", "Gmail:UsernameOrEmail");
        var password = Get("Smtp:Password", "Gmail:Password");
        var fromEmail = Get("Smtp:FromEmail", "Gmail:FromEmail", userName);
        var fromName = Get("Smtp:FromName", "Gmail:FromName", "TPG LLC");
        var useSsl = GetBool("Smtp:UseSsl", "Gmail:UseSsl", true);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP settings are missing. Set Smtp:* or Gmail:* configuration values.");
        }

        return SendAsync(host, port, useSsl, userName, password, fromEmail, fromName, email, subject, htmlMessage);
    }

    private async Task SendAsync(string host, int port, bool useSsl, string userName, string password, string fromEmail, string fromName, string toEmail, string subject, string htmlMessage)
    {
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            Credentials = new NetworkCredential(userName, password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);
        await client.SendMailAsync(message);
    }

    private string? Get(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private int GetInt(string key1, string key2, int defaultValue)
        => int.TryParse(Get(key1, key2), out var value) ? value : defaultValue;

    private bool GetBool(string key1, string key2, bool defaultValue)
        => bool.TryParse(Get(key1, key2), out var value) ? value : defaultValue;
}

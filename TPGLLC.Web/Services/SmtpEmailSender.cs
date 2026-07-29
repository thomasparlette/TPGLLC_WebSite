using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace TPGLLC.Web.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private readonly IConfiguration _configuration = configuration;

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = Get("Smtp:Host", "Gmail:Host");
        var port = GetInt(587, "Smtp:Port", "Gmail:Port");
        var userName = Get("Smtp:UserName", "Gmail:UserName", "Gmail:Username", "Gmail:UsernameOrEmail");
        var password = Get("Smtp:Password", "Gmail:Password");
        var fromEmail = Get("Smtp:FromEmail", "Gmail:FromEmail", userName);
        var fromName = Get("Smtp:FromName", "Gmail:FromName", "TPG LLC");
        var useSsl = GetBool(true, "Smtp:UseSsl", "Gmail:UseSsl");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP settings are missing or incomplete.");
        }

        return SendAsync(
            host,
            port,
            useSsl,
            userName,
            password,
            fromEmail,
            fromName ?? string.Empty,
            email,
            subject,
            htmlMessage);
    }

    private static async Task SendAsync(
        string host,
        int port,
        bool useSsl,
        string userName,
        string password,
        string fromEmail,
        string fromName,
        string email,
        string subject,
        string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(host, port, useSsl).ConfigureAwait(false);
        await client.AuthenticateAsync(userName, password).ConfigureAwait(false);
        await client.SendAsync(message).ConfigureAwait(false);
        await client.DisconnectAsync(true).ConfigureAwait(false);
    }

    private string? Get(params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private int GetInt(int fallback, params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var raw = _configuration[key];
            if (int.TryParse(raw, out var value))
            {
                return value;
            }
        }

        return fallback;
    }

    private bool GetBool(bool fallback, params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var raw = _configuration[key];
            if (bool.TryParse(raw, out var value))
            {
                return value;
            }
        }

        return fallback;
    }
}
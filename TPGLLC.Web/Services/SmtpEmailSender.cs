using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace TPGLLC.Web.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    private readonly IConfiguration _configuration = configuration;

    public Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        var host = Get(
            "Smtp:Host",
            "Gmail:Host");

        var port = GetInt(
            587,
            "Smtp:Port",
            "Gmail:Port");

        var userName = Get(
            "Smtp:UserName",
            "Gmail:UserName",
            "Gmail:Username",
            "Gmail:UsernameOrEmail");

        var password = Get(
            "Smtp:Password",
            "Gmail:Password");

        var fromEmail = Get(
            "Smtp:FromEmail",
            "Smtp:FromAddress",
            "Gmail:FromEmail",
            "Gmail:FromAddress",
            userName);

        var fromName = Get(
            "Smtp:FromName",
            "Gmail:FromName",
            "TPG LLC");

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "SMTP settings are missing or incomplete. " +
                "Check Host, Port, Username, Password, and FromAddress settings.");
        }

        var socketOptions = GetSocketOptions(port);

        return SendAsync(
            host,
            port,
            socketOptions,
            userName,
            password,
            fromEmail,
            fromName ?? "TPG LLC",
            email,
            subject,
            htmlMessage);
    }

    private static async Task SendAsync(
        string host,
        int port,
        SecureSocketOptions socketOptions,
        string userName,
        string password,
        string fromEmail,
        string fromName,
        string email,
        string subject,
        string htmlMessage)
    {
        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                fromName,
                fromEmail));

        message.To.Add(
            MailboxAddress.Parse(email));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlMessage
        }.ToMessageBody();

        using var client =
            new MailKit.Net.Smtp.SmtpClient();

        await client.ConnectAsync(
            host,
            port,
            socketOptions);

        await client.AuthenticateAsync(
            userName,
            password);

        await client.SendAsync(message);

        await client.DisconnectAsync(true);
    }

    private SecureSocketOptions GetSocketOptions(int port)
    {
        var configured = Get(
            "Smtp:Security",
            "Gmail:Security");

        if (!string.IsNullOrWhiteSpace(configured))
        {
            switch (configured.Trim().ToLowerInvariant())
            {
                case "starttls":
                    return SecureSocketOptions.StartTls;

                case "starttlswhenavailable":
                    return SecureSocketOptions.StartTlsWhenAvailable;

                case "ssl":
                case "sslonconnect":
                    return SecureSocketOptions.SslOnConnect;

                case "none":
                    return SecureSocketOptions.None;

                case "auto":
                    return SecureSocketOptions.Auto;
            }
        }

        // Standard SMTP defaults:
        //
        // 465 = implicit SSL/TLS
        // 587 = STARTTLS
        //
        // Default to STARTTLS for everything except
        // the standard implicit-TLS port.

        return port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
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

    private int GetInt(
        int fallback,
        params string?[] keys)
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
}
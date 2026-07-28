using System.Net;
using Microsoft.Extensions.Hosting;

namespace TPGLLC.Web.Services.Appointments;

public interface IEmailTemplateRenderer
{
    string Render(string templateName, IReadOnlyDictionary<string, string?> tokens);
}

public sealed class FileEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IWebHostEnvironment _env;

    public FileEmailTemplateRenderer(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string Render(string templateName, IReadOnlyDictionary<string, string?> tokens)
    {
        var path = Path.Combine(_env.ContentRootPath, "EmailTemplates", templateName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Email template not found: {path}");
        }

        var html = File.ReadAllText(path);

        foreach (var token in tokens)
        {
            html = html.Replace(
                "{{" + token.Key + "}}",
                WebUtility.HtmlEncode(token.Value ?? string.Empty),
                StringComparison.OrdinalIgnoreCase);
        }

        return html;
    }
}
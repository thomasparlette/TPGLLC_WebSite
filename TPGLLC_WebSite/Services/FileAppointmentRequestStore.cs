using System.Text.Json;
using TPGLLC_WebSite.Models;

namespace TPGLLC_WebSite.Services;

public sealed class FileAppointmentRequestStore : IAppointmentRequestStore
{
    private readonly IWebHostEnvironment _env;

    public FileAppointmentRequestStore(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task SaveAsync(AppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var inboxDir = Path.Combine(_env.ContentRootPath, "App_Data", "AppointmentRequests");
        Directory.CreateDirectory(inboxDir);

        var fileName = $"{request.SubmittedAtUtc:yyyyMMdd_HHmmss}_{request.RequestId:N}.json";
        var filePath = Path.Combine(inboxDir, fileName);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }
}
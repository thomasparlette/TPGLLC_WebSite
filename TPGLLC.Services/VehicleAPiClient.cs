using System.Net.Http.Json;
using TPGLLC.Shared.Models;

namespace TPGLLC.Web.Services;

public sealed class VehicleApiClient
{
    private readonly HttpClient _http;

    public VehicleApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<string>> GetVehicleTypesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<List<string>>(
            "api/vehicle-types",
            cancellationToken);

        return result ?? [];
    }

    public async Task<IReadOnlyList<int>> GetYearsAsync(string vehicleType, CancellationToken cancellationToken = default)
    {
        var url = $"api/vehicle-years?vehicleType={Uri.EscapeDataString(vehicleType)}";
        var result = await _http.GetFromJsonAsync<List<int>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<string>> GetMakesAsync(string vehicleType, int year, CancellationToken cancellationToken = default)
    {
        var url =
            $"api/vehicle-makes?vehicleType={Uri.EscapeDataString(vehicleType)}&year={year}";
        var result = await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(string vehicleType, int year, string make, CancellationToken cancellationToken = default)
    {
        var url =
            $"api/vehicle-models?vehicleType={Uri.EscapeDataString(vehicleType)}&year={year}&make={Uri.EscapeDataString(make)}";
        var result = await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<Guid?> SubmitAppointmentAsync(AppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/appointments", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<AppointmentCreatedResponse>(cancellationToken: cancellationToken);
        return payload?.RequestId;
    }

    private sealed class AppointmentCreatedResponse
    {
        public Guid RequestId { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset SubmittedAtUtc { get; set; }
    }
}
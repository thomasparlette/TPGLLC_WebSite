using System.Net.Http.Json;

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
            "api/v1/vehicles/types",
            cancellationToken);

        return result ?? [];
    }

    public async Task<IReadOnlyList<int>> GetYearsAsync(
        string vehicleType,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/vehicles/years?vehicleType={Uri.EscapeDataString(vehicleType)}";
        var result = await _http.GetFromJsonAsync<List<int>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<string>> GetMakesAsync(
        string vehicleType,
        int year,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/vehicles/makes?vehicleType={Uri.EscapeDataString(vehicleType)}&year={year}";
        var result = await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(
        string vehicleType,
        int year,
        string make,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"api/v1/vehicles/models?vehicleType={Uri.EscapeDataString(vehicleType)}&year={year}&make={Uri.EscapeDataString(make)}";

        var result = await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);
        return result ?? [];
    }
}
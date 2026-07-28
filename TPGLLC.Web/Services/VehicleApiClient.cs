using System.Net.Http.Json;

namespace TPGLLC.Web.Services;

public sealed class VehicleApiClient
{
    private readonly HttpClient _http;

    public VehicleApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<int>>(
                "api/v1/vehicles/years",
                cancellationToken);

            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetMakesAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/v1/vehicles/makes?year={year}";
            var result = await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(
        int year,
        string make,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/v1/vehicles/models?year={year}&make={Uri.EscapeDataString(make)}";
            var result = await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }
}
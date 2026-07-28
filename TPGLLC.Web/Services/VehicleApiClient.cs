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
        try
        {
            var result = await _http.GetFromJsonAsync<List<string>>(
                "api/v1/vehicles/types",
                cancellationToken);

            if (result is { Count: > 0 })
                return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        // Fallback so production still works
        return
        [
            "Automotive",
            "Motorcycle"
        ];
    }

    public async Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                $"api/v1/vehicles/years";

            var result =
                await _http.GetFromJsonAsync<List<int>>(url, cancellationToken);

            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetMakesAsync(int year,CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                $"api/v1/vehicles/makes?year={year}";

            var result =
                await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);

            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetModelsAsync(int year,string make,CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                $"api/v1/vehicles/models?year={year}&make={Uri.EscapeDataString(make)}";

            var result =
                await _http.GetFromJsonAsync<List<string>>(url, cancellationToken);

            return result ?? [];
        }
        catch
        {
            return [];
        }
    }
}
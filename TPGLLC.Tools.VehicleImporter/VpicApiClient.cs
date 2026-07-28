using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TPGLLC.Tools.VehicleImporter;

public interface IVpicApiClient
{
    Task<IReadOnlyList<VpicMakeDto>> GetMakesForVehicleTypeAsync(
        string vehicleTypeSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VpicModelDto>> GetModelsForMakeIdYearAsync(
        int makeId,
        int modelYear,
        string? vehicleTypeSlug = null,
        CancellationToken cancellationToken = default);
}

public sealed class VpicApiClient : IVpicApiClient
{
    private readonly HttpClient _http;

    public VpicApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<VpicMakeDto>> GetMakesForVehicleTypeAsync(
        string vehicleTypeSlug,
        CancellationToken cancellationToken = default)
    {
        var url = $"vehicles/GetMakesForVehicleType/{Uri.EscapeDataString(vehicleTypeSlug)}?format=json";

        var response = await _http.GetFromJsonAsync<VpicResponse<VpicMakeDto>>(url, cancellationToken);
        return response?.Results ?? [];
    }

    public async Task<IReadOnlyList<VpicModelDto>> GetModelsForMakeIdYearAsync(
    int makeId,
    int modelYear,
    CancellationToken cancellationToken = default)
{
    var url = $"vehicles/GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{modelYear}?format=json";

    var response = await _http.GetFromJsonAsync<VpicResponse<VpicModelDto>>(url, cancellationToken);
    return response?.Results ?? [];
}
}

public sealed class VpicResponse<T>
{
    public int Count { get; set; }
    public string? Message { get; set; }
    public string? SearchCriteria { get; set; }
    public List<T> Results { get; set; } = [];
}

public sealed class VpicMakeDto
{
    [JsonPropertyName("MakeId")]
    public int MakeId { get; set; }

    [JsonPropertyName("MakeName")]
    public string MakeName { get; set; } = string.Empty;

    [JsonPropertyName("VehicleTypeId")]
    public int VehicleTypeId { get; set; }

    [JsonPropertyName("VehicleTypeName")]
    public string VehicleTypeName { get; set; } = string.Empty;
}

public sealed class VpicModelDto
{
    [JsonPropertyName("Make_ID")]
    public int MakeId { get; set; }

    [JsonPropertyName("Make_Name")]
    public string MakeName { get; set; } = string.Empty;

    [JsonPropertyName("Model_ID")]
    public int ModelId { get; set; }

    [JsonPropertyName("Model_Name")]
    public string ModelName { get; set; } = string.Empty;
}

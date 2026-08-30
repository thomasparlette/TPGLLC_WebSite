using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TPGLLC.Web.Services.Vehicles;

public sealed class VpicVehicleDecoder : IVpicVehicleDecoder
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VpicVehicleDecoder> _logger;

    public VpicVehicleDecoder(
        HttpClient httpClient,
        ILogger<VpicVehicleDecoder> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<VpicVehicleDecodeResult> DecodeAsync(
        string vin,
        CancellationToken cancellationToken = default)
    {
        var normalizedVin = NormalizeVin(vin);

        if (normalizedVin.Length < 11)
        {
            return VpicVehicleDecodeResult.Failure(
                "Enter at least 11 VIN characters before decoding.");
        }

        try
        {
            var path = $"vehicles/DecodeVinExtended/{Uri.EscapeDataString(normalizedVin)}?format=json";
            var response = await _httpClient.GetFromJsonAsync<VpicApiResponse>(path, cancellationToken);

            if (response?.Results is null || response.Results.Count == 0)
            {
                return VpicVehicleDecodeResult.Failure(
                    "VPIC did not return vehicle information for this VIN.");
            }

            var rows = response.Results
                .Where(x => !string.IsNullOrWhiteSpace(x.Variable))
                .Select(x => new VpicRow(x.Variable!, CleanValue(x.Value)))
                .Where(x => x.Value is not null)
                .ToList();

            var result = new VpicVehicleDecodeResult
            {
                Succeeded = true,
                Message = "Vehicle information decoded from VPIC.",
                ModelYearOptions = Values(rows, "Model Year"),
                MakeOptions = Values(rows, "Make"),
                ModelOptions = Values(rows, "Model"),
                SubmodelOptions = Values(rows, "Trim", "Series", "Series2"),
                BodyStyleOptions = Values(rows, "Body Class"),
                EngineFuelOptions = BuildEngineOptions(rows),
                TransmissionOptions = BuildTransmissionOptions(rows),
                DriveTypeOptions = Values(rows, "Drive Type", "Axles"),
                BrakeOptions = Values(rows, "Brake System Type", "Brake System Type Other"),
                GvwOptions = Values(rows,
                    "Gross Vehicle Weight Rating From",
                    "Gross Vehicle Weight Rating To",
                    "Gross Vehicle Weight Rating")
            };

            if (!HasVehicleData(result))
            {
                return VpicVehicleDecodeResult.Failure(
                    "VPIC did not return usable vehicle information for this VIN.");
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "VPIC VIN decode failed for {Vin}.", normalizedVin);
            return VpicVehicleDecodeResult.Failure(
                "VPIC could not be reached right now. You can continue entering the vehicle details manually.");
        }
    }

    private static IReadOnlyList<string> Values(
        IReadOnlyCollection<VpicRow> rows,
        params string[] variables)
    {
        return rows
            .Where(row => variables.Any(variable =>
                string.Equals(row.Variable, variable, StringComparison.OrdinalIgnoreCase)))
            .Select(row => row.Value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> BuildEngineOptions(IReadOnlyCollection<VpicRow> rows)
    {
        var engineModels = Values(rows, "Engine Model");
        var displacement = Values(rows, "Displacement (L)", "Displacement (CC)");
        var cylinders = Values(rows, "Engine Number of Cylinders");
        var fuel = Values(rows, "Fuel Type - Primary");

        var descriptions = engineModels
            .Select(model => string.Join(" / ", new[]
            {
                model,
                displacement.FirstOrDefault(),
                cylinders.FirstOrDefault() is { } count ? $"{count} cyl" : null,
                fuel.FirstOrDefault()
            }.Where(value => !string.IsNullOrWhiteSpace(value))))
            .ToList();

        if (descriptions.Count == 0)
        {
            descriptions = displacement
                .Select(value => string.Join(" / ", new[]
                {
                    value,
                    cylinders.FirstOrDefault() is { } count ? $"{count} cyl" : null,
                    fuel.FirstOrDefault()
                }.Where(item => !string.IsNullOrWhiteSpace(item))))
                .ToList();
        }

        if (descriptions.Count == 0)
        {
            descriptions = fuel.ToList();
        }

        return descriptions
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> BuildTransmissionOptions(IReadOnlyCollection<VpicRow> rows)
    {
        var styles = Values(rows, "Transmission Style");
        var speeds = Values(rows, "Transmission Speeds");

        var descriptions = styles
            .Select(style => string.Join(" / ", new[]
            {
                style,
                speeds.FirstOrDefault() is { } speed ? $"{speed}-speed" : null
            }.Where(value => !string.IsNullOrWhiteSpace(value))))
            .ToList();

        return descriptions.Count > 0
            ? descriptions
            : speeds.ToList();
    }

    private static bool HasVehicleData(VpicVehicleDecodeResult result) =>
        result.ModelYearOptions.Count > 0
        || result.MakeOptions.Count > 0
        || result.ModelOptions.Count > 0
        || result.SubmodelOptions.Count > 0
        || result.BodyStyleOptions.Count > 0
        || result.EngineFuelOptions.Count > 0
        || result.TransmissionOptions.Count > 0
        || result.DriveTypeOptions.Count > 0
        || result.BrakeOptions.Count > 0
        || result.GvwOptions.Count > 0;

    private static string NormalizeVin(string? vin) =>
        new string((vin ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character))
            .ToArray())
            .ToUpperInvariant();

    private static string? CleanValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.Trim();
        return cleaned.Equals("Not Applicable", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("Not Available", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            || cleaned.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : cleaned;
    }

    private sealed class VpicApiResponse
    {
        [JsonPropertyName("Results")]
        public List<VpicApiRow> Results { get; set; } = [];
    }

    private sealed class VpicApiRow
    {
        [JsonPropertyName("Variable")]
        public string? Variable { get; set; }

        [JsonPropertyName("Value")]
        public string? Value { get; set; }
    }

    private sealed record VpicRow(string Variable, string? Value);
}

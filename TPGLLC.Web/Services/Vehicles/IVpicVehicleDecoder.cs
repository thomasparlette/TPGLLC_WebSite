namespace TPGLLC.Web.Services.Vehicles;

public interface IVpicVehicleDecoder
{
    Task<VpicVehicleDecodeResult> DecodeAsync(
        string vin,
        CancellationToken cancellationToken = default);
}

public sealed class VpicVehicleDecodeResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }

    public IReadOnlyList<string> ModelYearOptions { get; init; } = [];
    public IReadOnlyList<string> MakeOptions { get; init; } = [];
    public IReadOnlyList<string> ModelOptions { get; init; } = [];
    public IReadOnlyList<string> SubmodelOptions { get; init; } = [];
    public IReadOnlyList<string> BodyStyleOptions { get; init; } = [];
    public IReadOnlyList<string> EngineFuelOptions { get; init; } = [];
    public IReadOnlyList<string> TransmissionOptions { get; init; } = [];
    public IReadOnlyList<string> DriveTypeOptions { get; init; } = [];
    public IReadOnlyList<string> BrakeOptions { get; init; } = [];
    public IReadOnlyList<string> GvwOptions { get; init; } = [];

    public string? GetOnly(IReadOnlyList<string> options) =>
        options.Count == 1 ? options[0] : null;

    public static VpicVehicleDecodeResult Failure(string message) => new()
    {
        Succeeded = false,
        Message = message
    };
}

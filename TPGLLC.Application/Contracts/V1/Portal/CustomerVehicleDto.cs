namespace TPGLLC.Application.Contracts.V1.Portal;

public sealed record CustomerVehicleDto(
    Guid Id,
    string DisplayName,
    string Year,
    string Make,
    string Model,
    string? Trim,
    string? Vin,
    string? Mileage,
    bool IsPrimary,
    string? Notes);
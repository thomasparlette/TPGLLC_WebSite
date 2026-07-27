namespace TPGLLC.Application.Contracts.V1.Portal;

public sealed record ServiceHistoryEntryDto(
    Guid Id,
    string VehicleName,
    DateOnly Date,
    string Service,
    int Mileage,
    string Technician,
    string Status);
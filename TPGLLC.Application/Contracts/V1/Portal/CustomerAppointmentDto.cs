namespace TPGLLC.Application.Contracts.V1.Portal;

public sealed record CustomerAppointmentDto(
    Guid Id,
    string VehicleName,
    string Service,
    DateOnly Date,
    TimeOnly Time,
    string Status,
    string Notes);
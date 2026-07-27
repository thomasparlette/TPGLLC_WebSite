namespace TPGLLC.Application.Contracts.V1.Portal;

public sealed record InvoiceEntryDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly Date,
    string VehicleName,
    decimal Total,
    bool Paid,
    string Status);
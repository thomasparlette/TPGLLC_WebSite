namespace TPGLLC.Application.Contracts.V1.Portal;

public sealed record CustomerProfileDto(
    string DisplayName,
    string Email,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode);
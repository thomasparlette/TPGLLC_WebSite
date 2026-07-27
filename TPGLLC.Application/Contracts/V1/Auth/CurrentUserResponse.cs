namespace TPGLLC.Application.Contracts.V1.Auth;

public sealed record CurrentUserResponse(
    string UserId,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
namespace TPGLLC.Application.Authentication;

public sealed record CurrentUserResponse(
    string UserId,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
namespace TPGLLC.Services.Authentication;

public sealed record CurrentUserSnapshot(
    string UserId,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
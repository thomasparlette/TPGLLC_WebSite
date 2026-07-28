namespace TPGLLC.Application.Authentication;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string UserId,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
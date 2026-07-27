namespace TPGLLC.Services.Security;

public sealed record AuthenticationSession(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string UserId,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
namespace TPGLLC.Application.Contracts.V1.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string UserId,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
namespace TPGLLC.Services.Security;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresUtc,
    string JwtId);
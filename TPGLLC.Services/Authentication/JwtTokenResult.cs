namespace TPGLLC.Services.Authentication;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresUtc,
    string JwtId);
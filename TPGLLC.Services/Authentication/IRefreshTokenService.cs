using TPGLLC.Shared.Identity;

namespace TPGLLC.Services.Authentication;

public interface IRefreshTokenService
{
    Task<string> IssueAsync(
        string userId,
        string jwtId,
        string? deviceName,
        string? ipAddress,
        DateTimeOffset expiresUtc,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> FindActiveByRawTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default);
}
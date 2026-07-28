using System.Security.Claims;

namespace TPGLLC.Services.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationSession?> RegisterAsync(
        string email,
        string password,
        string? displayName,
        string? ipAddress,
        string? deviceName,
        CancellationToken cancellationToken = default);

    Task<AuthenticationSession?> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        string? deviceName,
        CancellationToken cancellationToken = default);

    Task<AuthenticationSession?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? deviceName,
        CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<CurrentUserSnapshot?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
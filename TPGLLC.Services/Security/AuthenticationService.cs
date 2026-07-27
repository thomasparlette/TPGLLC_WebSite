using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Services.Security;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtOptions _options;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtOptions> options)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _options = options.Value;
    }

    public async Task<AuthenticationSession?> RegisterAsync(
        string email,
        string password,
        string? displayName,
        string? ipAddress,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return null;
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            IsActive = true,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return null;
        }

        if (await _roleManager.RoleExistsAsync("Customer") && !await _userManager.IsInRoleAsync(user, "Customer"))
        {
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        return await CreateSessionAsync(user, ipAddress, deviceName, cancellationToken);
    }

    public async Task<AuthenticationSession?> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordOk)
        {
            await _userManager.AccessFailedAsync(user);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        return await CreateSessionAsync(user, ipAddress, deviceName, cancellationToken);
    }

    public async Task<AuthenticationSession?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenService.FindActiveByRawTokenAsync(refreshToken, cancellationToken);
        if (token is null)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(token.UserId);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        await _refreshTokenService.RevokeAsync(token, cancellationToken);
        user.LastLoginUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        return await CreateSessionAsync(user, ipAddress, deviceName, cancellationToken);
    }

    public async Task<bool> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var token = await _refreshTokenService.FindActiveByRawTokenAsync(refreshToken, cancellationToken);
        if (token is null)
        {
            return false;
        }

        await _refreshTokenService.RevokeAsync(token, cancellationToken);
        return true;
    }

    public async Task<CurrentUserSnapshot?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new CurrentUserSnapshot(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray());
    }

    private async Task<AuthenticationSession> CreateSessionAsync(
        ApplicationUser user,
        string? ipAddress,
        string? deviceName,
        CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jwt = await _jwtTokenService.CreateAccessTokenAsync(user, roles, cancellationToken);

        var refreshTokenExpiresUtc = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);
        var refreshToken = await _refreshTokenService.IssueAsync(
            user.Id,
            jwt.JwtId,
            deviceName,
            ipAddress,
            refreshTokenExpiresUtc,
            cancellationToken);

        return new AuthenticationSession(
            jwt.AccessToken,
            refreshToken,
            jwt.ExpiresUtc,
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray());
    }
}
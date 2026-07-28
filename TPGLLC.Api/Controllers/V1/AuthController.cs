using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPGLLC.Application.Authentication;
using TPGLLC.Services.Authentication;

namespace TPGLLC.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _authenticationService.RegisterAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            GetIpAddress(),
            GetUserAgent(),
            cancellationToken);

        if (session is null)
        {
            return BadRequest(new { message = "Unable to create the account." });
        }

        return Ok(ToResponse(session));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _authenticationService.LoginAsync(
            request.Email,
            request.Password,
            GetIpAddress(),
            GetUserAgent(),
            cancellationToken);

        if (session is null)
        {
            return Unauthorized(new { message = "Invalid login attempt." });
        }

        return Ok(ToResponse(session));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _authenticationService.RefreshAsync(
            request.RefreshToken,
            GetIpAddress(),
            GetUserAgent(),
            cancellationToken);

        if (session is null)
        {
            return Unauthorized(new { message = "Refresh token is invalid or expired." });
        }

        return Ok(ToResponse(session));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _authenticationService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await _authenticationService.GetCurrentUserAsync(User, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(
            user.UserId,
            user.Email,
            user.DisplayName,
            user.Roles));
    }

    private static AuthResponse ToResponse(AuthenticationSession session)
        => new(
            session.AccessToken,
            session.RefreshToken,
            session.AccessTokenExpiresUtc,
            session.UserId,
            session.Email,
            session.DisplayName,
            session.Roles);

    private string? GetIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
        => Request.Headers.UserAgent.ToString();
}
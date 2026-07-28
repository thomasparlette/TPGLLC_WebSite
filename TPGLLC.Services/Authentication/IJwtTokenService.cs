using TPGLLC.Shared.Identity;

namespace TPGLLC.Services.Authentication;

public interface IJwtTokenService
{
    Task<JwtTokenResult> CreateAccessTokenAsync(
        ApplicationUser user,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);
}
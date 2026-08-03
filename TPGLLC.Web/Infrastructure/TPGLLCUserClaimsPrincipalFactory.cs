using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Infrastructure;

public sealed class TPGLLCUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public TPGLLCUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var displayName =
            user.DisplayName?.Trim()
            ?? string.Join(" ", new[] { user.FirstName, user.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
                .Trim();

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            identity.AddClaim(new Claim("display_name", displayName));
        }

        return identity;
    }
}
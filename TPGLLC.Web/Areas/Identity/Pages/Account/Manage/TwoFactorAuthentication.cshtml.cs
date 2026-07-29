using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account.Manage;

public sealed class TwoFactorAuthenticationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public TwoFactorAuthenticationModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public bool HasAuthenticator { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public bool Is2FaEnabled { get; set; }
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        HasAuthenticator = !string.IsNullOrWhiteSpace(await _userManager.GetAuthenticatorKeyAsync(user));
        Is2FaEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        RecoveryCodesLeft = 0;
        return Page();
    }
}

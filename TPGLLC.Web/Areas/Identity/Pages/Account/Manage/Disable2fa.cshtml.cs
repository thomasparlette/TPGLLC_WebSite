using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account.Manage;

public sealed class Disable2faModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public Disable2faModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        return RedirectToPage("./TwoFactorAuthentication");
    }
}

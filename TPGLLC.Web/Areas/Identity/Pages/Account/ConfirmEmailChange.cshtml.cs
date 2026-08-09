using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class ConfirmEmailChangeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public ConfirmEmailChangeModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? userId,
        string? email,
        string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token))
        {
            StatusMessage =
                "The email confirmation link is invalid or incomplete.";

            return Page();
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            StatusMessage =
                "The account associated with this confirmation link could not be found.";

            return Page();
        }

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(token));
        }
        catch
        {
            StatusMessage =
                "The email confirmation token is invalid.";

            return Page();
        }

        var result = await _userManager.ChangeEmailAsync(
            user,
            email,
            decodedToken);

        if (!result.Succeeded)
        {
            StatusMessage = string.Join(
                " ",
                result.Errors.Select(x => x.Description));

            return Page();
        }

        var userNameResult = await _userManager.SetUserNameAsync(
            user,
            email);

        if (!userNameResult.Succeeded)
        {
            StatusMessage =
                "Your email address was confirmed, but your sign-in username could not be updated automatically.";

            return Page();
        }

        if (User.Identity?.IsAuthenticated == true &&
            User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id)
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        StatusMessage =
            "Thank you. Your new email address has been confirmed successfully.";

        return Page();
    }
}
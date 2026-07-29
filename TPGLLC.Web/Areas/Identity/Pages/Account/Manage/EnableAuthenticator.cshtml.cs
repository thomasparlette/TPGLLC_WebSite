using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account.Manage;

public sealed class EnableAuthenticatorModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public EnableAuthenticatorModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        await LoadAsync(user);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var code = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);

        if (!isValid)
        {
            ErrorMessage = "Verification code is invalid.";
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        StatusMessage = "Your authenticator app has been verified and 2FA is enabled.";
        ViewData["RecoveryCodes"] = recoveryCodes;
        return Page();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(key ?? string.Empty);
        var email = await _userManager.GetEmailAsync(user) ?? user.Email ?? user.UserName ?? "TPG LLC";
        AuthenticatorUri = BuildUri(email, key ?? string.Empty);
    }

    private static string FormatKey(string unformattedKey)
    {
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < unformattedKey.Length; i++)
        {
            builder.Append(unformattedKey[i]);
            if ((i + 1) % 4 == 0 && i + 1 < unformattedKey.Length)
            {
                builder.Append(' ');
            }
        }

        return builder.ToString();
    }

    private static string BuildUri(string email, string unformattedKey)
    {
        const string issuer = "TPG LLC";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={unformattedKey}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;
    }
}

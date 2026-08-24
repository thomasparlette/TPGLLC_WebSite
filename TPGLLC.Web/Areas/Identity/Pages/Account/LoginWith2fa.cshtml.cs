using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginWith2faModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null, bool rememberMe = false)
    {
        Input.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/portal" : returnUrl;
        Input.RememberMe = rememberMe;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        Input.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? Input.ReturnUrl : returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var code = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code,
            Input.RememberMe,
            Input.RememberMachine);

        if (result.Succeeded)
        {
            await RecordLoginAsync(user);
            return LocalRedirect(string.IsNullOrWhiteSpace(Input.ReturnUrl) ? "/portal" : Input.ReturnUrl!);
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "This account is locked. Please try again later.";
            return Page();
        }

        ErrorMessage = "Invalid authenticator code.";
        return Page();
    }

    public async Task<IActionResult> OnPostRecoveryCodeAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var code = Input.RecoveryCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded)
        {
            await RecordLoginAsync(user);
            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/portal" : returnUrl);
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "This account is locked. Please try again later.";
            return Page();
        }

        ErrorMessage = "Invalid recovery code.";
        return Page();
    }

    private async Task RecordLoginAsync(ApplicationUser? user)
    {
        if (user is null)
        {
            return;
        }

        user.LastLoginUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Authenticator code")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public bool RememberMachine { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

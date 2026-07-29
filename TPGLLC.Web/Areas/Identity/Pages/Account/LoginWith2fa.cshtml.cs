using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginWith2faModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
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
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code,
            Input.RememberMe,
            Input.RememberMachine);

        if (result.Succeeded)
        {
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
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded)
        {
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

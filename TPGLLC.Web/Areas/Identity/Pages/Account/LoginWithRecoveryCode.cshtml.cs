using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginWithRecoveryCodeModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        Input.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/portal" : returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        Input.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? Input.ReturnUrl : returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var code = Input.RecoveryCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(code);

        if (result.Succeeded)
        {
            return LocalRedirect(string.IsNullOrWhiteSpace(Input.ReturnUrl) ? "/portal" : Input.ReturnUrl!);
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
        [Display(Name = "Recovery code")]
        public string RecoveryCode { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
        if (user is null)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        var result = await _userManager.ResetPasswordAsync(user, code, Input.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join(" ", result.Errors.Select(x => x.Description)));
            return Page();
        }

        return RedirectToPage("./ResetPasswordConfirmation");
    }

    public sealed class InputModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(10)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

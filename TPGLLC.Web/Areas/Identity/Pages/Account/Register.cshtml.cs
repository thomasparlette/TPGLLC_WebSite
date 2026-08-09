using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<ApplicationRole> roleManager,
    IEmailSender emailSender) : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly IEmailSender _emailSender = emailSender;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        Input.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/portal/dashboard" : returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Input.Email.Trim();

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            ErrorMessage = "An account with this email already exists.";
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            DisplayName = Input.DisplayName.Trim(),
            IsActive = true,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(x => x.Description));
            return Page();
        }

        if (await _roleManager.RoleExistsAsync("Customer"))
        {
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        await SendConfirmationEmailAsync(user);

        await _signInManager.SignInAsync(user, isPersistent: true);
        return LocalRedirect(GetReturnUrl());
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, code = encodedCode },
            protocol: Request.Scheme);

        if (callbackUrl is null)
        {
            return;
        }

        await _emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your email address",
            $"Please confirm your email address by <a href='{callbackUrl}'>clicking here</a>.");
    }

    private string GetReturnUrl()
    {
        var returnUrl = Input.ReturnUrl;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return "/portal/dashboard";
    }

    public sealed class InputModel
    {
        [Required]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(10)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}

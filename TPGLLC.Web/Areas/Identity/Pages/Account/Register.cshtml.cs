using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        Input.ReturnUrl = GetReturnUrl(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        Input.ReturnUrl = GetReturnUrl(returnUrl ?? Input.ReturnUrl);

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
            DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName) ? null : Input.DisplayName.Trim(),
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

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, code = encodedCode, returnUrl = Input.ReturnUrl },
            protocol: Request.Scheme);

        if (callbackUrl is null)
        {
            ErrorMessage = "Unable to build the confirmation link.";
            return Page();
        }

        await _emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your email",
            $"Please confirm your account by <a href=\"{callbackUrl}\">clicking here</a>.");

        return RedirectToPage("./RegisterConfirmation", new
        {
            email,
            returnUrl = Input.ReturnUrl
        });
    }

    private string GetReturnUrl(string? returnUrl)
    {
        var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/portal" : returnUrl;
        if (!Url.IsLocalUrl(safeReturnUrl))
        {
            return "/portal";
        }

        return safeReturnUrl;
    }

    public sealed class InputModel
    {
        [Required]
        public string? DisplayName { get; set; }

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

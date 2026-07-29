using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Areas.Identity.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; private set; } = [];

    public async Task OnGetAsync(string? returnUrl = null)
    {
        Input.ReturnUrl = NormalizeReturnUrl(returnUrl);
        await LoadExternalProvidersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadExternalProvidersAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email.Trim(),
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(GetReturnUrl());
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "This account is locked. Please try again later.";
            return Page();
        }

        ErrorMessage = "Invalid email or password.";
        return Page();
    }

    public IActionResult OnPostExternalLogin(string provider, string? returnUrl = null)
    {
        var destination = NormalizeReturnUrl(returnUrl ?? Input.ReturnUrl);
        var redirectUrl = Url.Page(
            "/Identity/Account/Login",
            pageHandler: "ExternalLoginCallback",
            values: new { returnUrl = destination });

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    public async Task<IActionResult> OnGetExternalLoginCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        await LoadExternalProvidersAsync();

        var destination = NormalizeReturnUrl(returnUrl);
        Input.ReturnUrl = destination;

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            ErrorMessage = $"External sign-in failed: {remoteError}";
            return Page();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Unable to load external login information.";
            return Page();
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: true,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            return LocalRedirect(destination);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email)
            ?? info.Principal.FindFirstValue("email");

        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = "The external provider did not return an email address.";
            return Page();
        }

        var displayName = info.Principal.FindFirstValue(ClaimTypes.Name)
            ?? info.Principal.FindFirstValue("name")
            ?? email;

        var user = await _userManager.FindByEmailAsync(email.Trim());
        var isNewUser = user is null;

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email.Trim(),
                Email = email.Trim(),
                DisplayName = displayName.Trim(),
                IsActive = true,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                ErrorMessage = string.Join(" ", createResult.Errors.Select(x => x.Description));
                return Page();
            }
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded)
        {
            ErrorMessage = string.Join(" ", addLoginResult.Errors.Select(x => x.Description));
            return Page();
        }

        if (isNewUser && await _roleManager.RoleExistsAsync("Customer"))
        {
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return LocalRedirect(destination);
    }

    private async Task LoadExternalProvidersAsync()
    {
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return "/portal";
    }

    private string GetReturnUrl() => NormalizeReturnUrl(Input.ReturnUrl);

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }
    }
}

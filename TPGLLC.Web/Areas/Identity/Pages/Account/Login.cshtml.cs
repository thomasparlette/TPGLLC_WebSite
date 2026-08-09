using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
    private readonly IConfiguration _configuration;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public bool GoogleEnabled => IsConfigured("Authentication:Google:ClientId", "Authentication__Google__ClientId");
    public bool MicrosoftEnabled => IsConfigured("Authentication:Microsoft:ClientId", "Authentication__Microsoft__ClientId");

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

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email.Trim(),
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirect(await GetPostLoginReturnUrl(Input.ReturnUrl, Input.Email.Trim()));
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new
            {
                ReturnUrl = Input.ReturnUrl,
                RememberMe = Input.RememberMe
            });
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
        var callbackUrl = Url.Page("./Login", pageHandler: "Callback", values: new
        {
            returnUrl = GetReturnUrl(returnUrl)
        });

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl!);
        return Challenge(properties, provider);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        var safeReturnUrl = GetReturnUrl(returnUrl);

        if (!string.IsNullOrWhiteSpace(remoteError))
        {
            ErrorMessage = remoteError;
            Input.ReturnUrl = safeReturnUrl;
            return Page();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "External login information was not available.";
            Input.ReturnUrl = safeReturnUrl;
            return Page();
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email)
            ?? info.Principal.FindFirstValue("email")
            ?? info.Principal.FindFirstValue(ClaimTypes.Upn);

        var externalSignIn = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: true,
            bypassTwoFactor: true);

        if (externalSignIn.Succeeded)
        {
            return LocalRedirect(await GetPostLoginReturnUrl(safeReturnUrl, email ?? string.Empty));
        }

        if (externalSignIn.IsLockedOut)
        {
            ErrorMessage = "This account is locked. Please try again later.";
            Input.ReturnUrl = safeReturnUrl;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = "The external provider did not supply an email address.";
            Input.ReturnUrl = safeReturnUrl;
            return Page();
        }

        var lookupUser = await _userManager.FindByEmailAsync(email);
        if (lookupUser is null)
        {
            lookupUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = info.Principal.FindFirstValue(ClaimTypes.GivenName)
                    ?? info.Principal.FindFirstValue(ClaimTypes.Name)
                    ?? email,
                IsActive = true,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(lookupUser);
            if (!createResult.Succeeded)
            {
                ErrorMessage = string.Join(" ", createResult.Errors.Select(x => x.Description));
                Input.ReturnUrl = safeReturnUrl;
                return Page();
            }

            if (await _roleManager.RoleExistsAsync("Customer"))
            {
                await _userManager.AddToRoleAsync(lookupUser, "Customer");
            }
        }

        var addLoginResult = await _userManager.AddLoginAsync(lookupUser, info);
        if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(x => x.Code == "LoginAlreadyAssociated"))
        {
            ErrorMessage = string.Join(" ", addLoginResult.Errors.Select(x => x.Description));
            Input.ReturnUrl = safeReturnUrl;
            return Page();
        }

        await _signInManager.SignInAsync(lookupUser, isPersistent: true);
        return LocalRedirect(await GetPostLoginReturnUrl(safeReturnUrl, email));
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

    private async Task<string> GetPostLoginReturnUrl(string? requestedReturnUrl, string userNameOrEmail)
    {
        var safeReturnUrl = string.IsNullOrWhiteSpace(requestedReturnUrl) ? "/portal" : requestedReturnUrl;
        if (!Url.IsLocalUrl(safeReturnUrl))
        {
            safeReturnUrl = "/portal";
        }

        if (!string.Equals(safeReturnUrl, "/portal", StringComparison.OrdinalIgnoreCase))
        {
            return safeReturnUrl;
        }

        var lookupUser = await _userManager.FindByNameAsync(userNameOrEmail)
            ?? await _userManager.FindByEmailAsync(userNameOrEmail);

        if (lookupUser is null)
        {
            return "/portal";
        }

        var roles = await _userManager.GetRolesAsync(lookupUser);
        bool HasRole(string roleName) => roles.Any(role => string.Equals(role, roleName, StringComparison.OrdinalIgnoreCase));

        if (HasRole("Administrator"))
        {
            return "/portal/admin";
        }

        if (HasRole("ServiceAdvisor"))
        {
            return "/portal/employee/service-advisor";
        }

        if (HasRole("Technician"))
        {
            return "/portal/employee/technician";
        }

        if (HasRole("Finance"))
        {
            return "/portal/employee/finance";
        }

        return "/portal";
    }

    private bool IsConfigured(params string[] keys)
        => keys.Any(key => !string.IsNullOrWhiteSpace(_configuration[key]));

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

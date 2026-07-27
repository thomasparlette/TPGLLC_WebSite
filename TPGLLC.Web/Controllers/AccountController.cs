using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Web.Controllers;

[Route("account")]
public sealed class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    [HttpPost("login-pist")]
    public async Task<IActionResult> Login([FromForm] LoginForm model)
    {
        if (!ModelState.IsValid)
        {
            return Redirect("/account/login?error=invalid");
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return Redirect("/customer/dashboard");
        }

        if (result.IsLockedOut)
        {
            return Redirect("/account/login?error=locked");
        }

        return Redirect("/account/login?error=invalid");
    }

    [HttpPost("register-post")]
    public async Task<IActionResult> Register([FromForm] RegisterForm model)
    {
        if (!ModelState.IsValid)
        {
            return Redirect("/account/register?error=invalid");
        }

        var email = model.Email.Trim();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? null : model.DisplayName.Trim(),
            IsActive = true,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            return Redirect("/account/register?error=" +
                Uri.EscapeDataString(string.Join(" ", createResult.Errors.Select(x => x.Description))));
        }

        if (await _roleManager.RoleExistsAsync("Customer"))
        {
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return Redirect("/customer/dashboard");
    }

    [HttpPost("logout-post")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/");
    }

    public sealed class LoginForm
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = true;
    }

    public sealed class RegisterForm
    {
        public string? DisplayName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
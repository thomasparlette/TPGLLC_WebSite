using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TPGLLC.Data;
using TPGLLC.Shared.Identity;
using Microsoft.Extensions.Logging;

namespace TPGLLC.Tools.DatabaseBootstrapper;

public sealed class BootstrapRunner
{
    private readonly TPGLLCDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly BootstrapOptions _options;
    private readonly ILogger<BootstrapRunner> _logger;

    public BootstrapRunner(
        TPGLLCDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<BootstrapOptions> options,
        ILogger<BootstrapRunner> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting TPGLLC development bootstrapper...");

        _logger.LogInformation("Applying EF Core migrations...");
        await _db.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Seeding roles...");
        await SeedRolesAsync(cancellationToken);

        _logger.LogInformation("Seeding admin user...");
        await SeedAdminUserAsync(cancellationToken);

        _logger.LogInformation("Bootstrap complete.");
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in _options.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var exists = await _roleManager.RoleExistsAsync(roleName);
            if (exists)
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }

            _logger.LogInformation("Created role: {Role}", roleName);
        }
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var email = _options.AdminEmail.Trim();

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email
            };

            var createResult = await _userManager.CreateAsync(user, _options.AdminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create admin user '{email}': {string.Join("; ", createResult.Errors.Select(x => x.Description))}");
            }

            _logger.LogInformation("Created admin user: {Email}", email);
        }

        if (await _roleManager.RoleExistsAsync(_options.AdminRole) &&
            !await _userManager.IsInRoleAsync(user, _options.AdminRole))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, _options.AdminRole);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to add '{email}' to role '{_options.AdminRole}': {string.Join("; ", addRoleResult.Errors.Select(x => x.Description))}");
            }

            _logger.LogInformation("Added {Email} to role {Role}", email, _options.AdminRole);
        }
    }
}
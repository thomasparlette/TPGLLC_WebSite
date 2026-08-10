using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Data.Seed;

public static class IdentitySeed
{
    private sealed record RoleSeed(string Name, string Description);

    private static readonly RoleSeed[] DefaultRoles =
    [
        new("Administrator", "Full system access."),
        new("Owner", "Business owner access."),
        new("ServiceAdvisor", "Can manage customer service requests and work orders."),
        new("Technician", "Can view and update assigned work."),
        new("Finance", "Can manage invoices, payments, and billing."),
        new("Customer", "Customer portal access.")
    ];

    /*public static async Task SeedIdentityAsync(this IServiceProvider services,IConfiguration configuration,CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentitySeed");

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in DefaultRoles)
        {
            await EnsureRoleAsync(roleManager, role, logger, cancellationToken);
        }

        var adminEmail = GetSetting(configuration, "Identity:AdminEmail", "Identity__AdminEmail");
        var adminPassword = GetSetting(configuration, "Identity:AdminPassword", "Identity__AdminPassword");
        var adminDisplayName = GetSetting(configuration, "Identity:AdminDisplayName", "Identity__AdminDisplayName") ?? "Administrator";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Admin seed skipped. Set Identity__AdminEmail and Identity__AdminPassword environment variables to create the first administrator.");
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = adminDisplayName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                ThrowIdentityFailure("create administrator user", createResult.Errors);
            }

            logger.LogInformation("Created initial administrator account for {Email}.", adminEmail);
        }
        else
        {
            var changed = false;

            if (!string.Equals(adminUser.DisplayName, adminDisplayName, StringComparison.Ordinal))
            {
                adminUser.DisplayName = adminDisplayName;
                changed = true;
            }

            if (!adminUser.IsActive)
            {
                adminUser.IsActive = true;
                changed = true;
            }

            if (!adminUser.EmailConfirmed)
            {
                adminUser.EmailConfirmed = true;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(adminUser);
                if (!updateResult.Succeeded)
                {
                    ThrowIdentityFailure("update administrator user", updateResult.Errors);
                }
            }

            logger.LogInformation("Administrator account already exists for {Email}.", adminEmail);
        }

        await EnsureUserInRoleAsync(userManager, adminUser, "Administrator", logger, cancellationToken);
        await EnsureUserInRoleAsync(userManager, adminUser, "Owner", logger, cancellationToken);
    } */

    private static async Task EnsureRoleAsync(RoleManager<ApplicationRole> roleManager, RoleSeed roleSeed, ILogger logger, CancellationToken cancellationToken)
    {
        var existing = await roleManager.FindByNameAsync(roleSeed.Name);

        if (existing is null)
        {
            var created = await roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleSeed.Name,
                Description = roleSeed.Description,
                IsSystemRole = true
            });

            if (!created.Succeeded)
            {
                ThrowIdentityFailure($"create role '{roleSeed.Name}'", created.Errors);
            }

            logger.LogInformation("Created role {RoleName}.", roleSeed.Name);
            return;
        }

        var needsUpdate = false;

        if (!string.Equals(existing.Description, roleSeed.Description, StringComparison.Ordinal))
        {
            existing.Description = roleSeed.Description;
            needsUpdate = true;
        }

        if (!existing.IsSystemRole)
        {
            existing.IsSystemRole = true;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            var updated = await roleManager.UpdateAsync(existing);
            if (!updated.Succeeded)
            {
                ThrowIdentityFailure($"update role '{roleSeed.Name}'", updated.Errors);
            }
        }
    }

    private static async Task EnsureUserInRoleAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string roleName, ILogger logger, CancellationToken cancellationToken)
    {
        if (await userManager.IsInRoleAsync(user, roleName))
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            ThrowIdentityFailure($"add user '{user.Email}' to role '{roleName}'", result.Errors);
        }

        logger.LogInformation("Assigned {Email} to role {RoleName}.", user.Email, roleName);
    }

    private static string? GetSetting(IConfiguration configuration, string key, string envVar)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        value = Environment.GetEnvironmentVariable(envVar);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void ThrowIdentityFailure(string action, IEnumerable<IdentityError> errors)
    {
        var message = string.Join("; ", errors.Select(x => $"{x.Code}: {x.Description}"));
        throw new InvalidOperationException($"Failed to {action}. {message}");
    }
}
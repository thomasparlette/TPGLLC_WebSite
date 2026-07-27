using Microsoft.AspNetCore.Identity;

namespace TPGLLC.Shared.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginUtc { get; set; }
}
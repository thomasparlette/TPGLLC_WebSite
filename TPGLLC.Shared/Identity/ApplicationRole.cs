using Microsoft.AspNetCore.Identity;

namespace TPGLLC.Shared.Identity;

public sealed class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }
}
namespace TPGLLC.Shared.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string JwtId { get; set; } = string.Empty;

    public string? DeviceName { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresUtc { get; set; }

    public DateTimeOffset? RevokedUtc { get; set; }

    public bool IsRevoked => RevokedUtc.HasValue;

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresUtc;

    public bool IsActive => !IsRevoked && !IsExpired;
}
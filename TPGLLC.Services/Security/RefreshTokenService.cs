using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TPGLLC.Data;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Services.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly TPGLLCDbContext _db;
    private readonly JwtOptions _options;

    public RefreshTokenService(
        TPGLLCDbContext db,
        IOptions<JwtOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<string> IssueAsync(
        string userId,
        string jwtId,
        string? deviceName,
        string? ipAddress,
        DateTimeOffset expiresUtc,
        CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateRawToken();

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            JwtId = jwtId,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            CreatedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = expiresUtc
        };

        await _db.RefreshTokens.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    public async Task<RefreshToken?> FindActiveByRawTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = Hash(rawToken);

        var token = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        return token is not null && token.IsActive ? token : null;
    }

    public async Task RevokeAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Id == token.Id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.RevokedUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRawToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Hash(string rawToken)
    {
        using var sha = SHA512.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
namespace TPGLLC.Services.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "TPGLLC.Api";
    public string Audience { get; set; } = "TPGLLC.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
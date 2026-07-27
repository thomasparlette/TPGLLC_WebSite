using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Application.Contracts.V1.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
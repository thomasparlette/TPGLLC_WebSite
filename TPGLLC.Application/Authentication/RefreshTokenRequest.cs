using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Application.Authentication;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
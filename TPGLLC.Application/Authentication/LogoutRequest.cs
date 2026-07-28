using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Application.Authentication;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
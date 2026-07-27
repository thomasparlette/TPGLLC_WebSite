using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Application.Contracts.V1.Auth;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
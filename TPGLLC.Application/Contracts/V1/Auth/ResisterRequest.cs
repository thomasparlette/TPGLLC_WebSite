using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Application.Contracts.V1.Auth;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DisplayName { get; set; }
}
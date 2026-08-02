using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class CustomerAccountViewModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(120)]
    public string Company { get; set; } = string.Empty;

    [StringLength(25)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(120)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(120)]
    public string AddressLine2 { get; set; } = string.Empty;

    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [StringLength(40)]
    public string State { get; set; } = string.Empty;

    [StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public string DisplayName => $"{FirstName} {LastName}".Trim();
}
using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class CustomerAccountViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Display Name")]
    public string DisplayName =>
        $"{FirstName} {LastName}".Trim();

    [Display(Name = "Company")]
    public string Company { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Address Line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [Display(Name = "Address Line 2")]
    public string AddressLine2 { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    [Display(Name = "ZIP Code")]
    public string ZipCode { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public DateTimeOffset? LastLoginUtc { get; set; }

    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }
}
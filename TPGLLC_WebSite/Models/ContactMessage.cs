using System.ComponentModel.DataAnnotations;

namespace TPGLLC_WebSite.Models;

public sealed class ContactMessage
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Body { get; set; }

    // Honeypot field to reduce spam.
    public string? Company { get; set; }
}
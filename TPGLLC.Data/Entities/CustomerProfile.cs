using System.ComponentModel.DataAnnotations;
using TPGLLC.Shared.Identity;

namespace TPGLLC.Data.Entities;

public sealed class CustomerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(450)]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(120)]
    public string? Company { get; set; }

    [MaxLength(150)]
    public string? Address1 { get; set; }

    [MaxLength(150)]
    public string? Address2 { get; set; }

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(40)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(80)]
    public string? Country { get; set; }

    [MaxLength(30)]
    public string? PreferredContactMethod { get; set; }

    public bool ReceiveEmail { get; set; } = true;

    public bool ReceiveSms { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedUtc { get; set; }

    public ApplicationUser? User { get; set; }
}
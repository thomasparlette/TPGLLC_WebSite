using System.ComponentModel.DataAnnotations;
using TPGLLC.Data.Entities;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class EstimateCatalogPageViewModel
{
    public List<PartsCatalogItem> Parts { get; set; } = [];
    public List<LaborCatalogItem> Labor { get; set; } = [];
    public PartsCatalogEditViewModel PartForm { get; set; } = new();
    public LaborCatalogEditViewModel LaborForm { get; set; } = new();
    public Guid? EditingPartId { get; set; }
    public Guid? EditingLaborId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public sealed class PartsCatalogEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Part number")]
    public string PartNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Display(Name = "Unit cost")]
    public string? UnitCost { get; set; }

    [Required]
    [Display(Name = "Retail price")]
    public string UnitPrice { get; set; } = "0.00";

    public bool IsActive { get; set; } = true;
}

public sealed class LaborCatalogEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Standard hours")]
    public string DefaultHours { get; set; } = "1.00";

    [Required]
    [Display(Name = "Hourly rate")]
    public string HourlyRate { get; set; } = "0.00";

    public bool IsActive { get; set; } = true;
}

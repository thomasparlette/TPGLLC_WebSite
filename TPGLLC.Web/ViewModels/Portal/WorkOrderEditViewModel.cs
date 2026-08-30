using System.ComponentModel.DataAnnotations;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class WorkOrderEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Work order number")]
    public string WorkOrderNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Service date")]
    public DateTime ServiceDate { get; set; } = DateTime.Today;

    [Required]
    [StringLength(200)]
    [Display(Name = "Vehicle")]
    public string VehicleName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Primary concern")]
    public string Service { get; set; } = string.Empty;

    [StringLength(4000)]
    [Display(Name = "Customer complaint")]
    public string? Complaint { get; set; }

    [StringLength(4000)]
    [Display(Name = "Diagnosis")]
    public string? Diagnosis { get; set; }

    [StringLength(120)]
    [Display(Name = "Technician")]
    public string? Technician { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Quoted";

    [StringLength(30)]
    [Display(Name = "Customer approval")]
    public string? ApprovalStatus { get; set; }

    [Display(Name = "Mileage")]
    public string? Mileage { get; set; }

    [Display(Name = "Mileage out")]
    public string? MileageOut { get; set; }

    [Display(Name = "Estimate")]
    public string? EstimateAmount { get; set; }

    [Display(Name = "Labor")]
    public string? LaborAmount { get; set; }

    [StringLength(50)]
    [Display(Name = "Invoice number")]
    public string? InvoiceNumber { get; set; }

    [Display(Name = "Invoice total")]
    public string? InvoiceAmount { get; set; }

    [StringLength(4000)]
    [Display(Name = "Customer notes")]
    public string? Notes { get; set; }

    [StringLength(4000)]
    [Display(Name = "Internal notes")]
    public string? InternalNotes { get; set; }

    public List<WorkOrderJobEditViewModel> Jobs { get; set; } = [];
    public List<WorkOrderInspectionEditViewModel> Inspections { get; set; } = [];
    public List<WorkOrderPartEditViewModel> Parts { get; set; } = [];
}

public sealed class WorkOrderJobEditViewModel
{
    public Guid Id { get; set; }
    public Guid? LaborCatalogItemId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Proposed";

    public string? LaborHours { get; set; }
    public string? LaborRate { get; set; }
    public string? LaborAmount { get; set; }
    public bool IsApproved { get; set; }
    public bool IsCustomerDeclined { get; set; }
    public bool IsDeferred { get; set; }
}

public sealed class WorkOrderInspectionEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Area { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Condition { get; set; } = "Good";

    [Required]
    [StringLength(4000)]
    public string Finding { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Recommendation { get; set; }

    public bool IsCustomerVisible { get; set; } = true;
}

public sealed class WorkOrderPartEditViewModel
{
    public Guid Id { get; set; }
    public Guid? ServiceHistoryJobId { get; set; }
    public Guid? PartsCatalogItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Quantity { get; set; } = "1";
    public string? UnitPrice { get; set; }
    public bool IsApplied { get; set; }
    public bool IsApproved { get; set; }
    public bool IsCustomerDeclined { get; set; }
}

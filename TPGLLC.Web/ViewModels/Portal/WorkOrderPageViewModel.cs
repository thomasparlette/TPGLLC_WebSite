using TPGLLC.Data.Entities;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class WorkOrderPageViewModel
{
    public List<ServiceHistoryEntry> WorkOrders { get; set; } = [];
    public List<TechnicianOptionViewModel> TechnicianOptions { get; set; } = [];
    public List<PartsCatalogOptionViewModel> PartsCatalogOptions { get; set; } = [];
    public List<LaborCatalogOptionViewModel> LaborCatalogOptions { get; set; } = [];
    public WorkOrderEditViewModel Form { get; set; } = new();
    public Guid? EditingWorkOrderId { get; set; }
    public List<string> StatusOptions { get; set; } = WorkOrderStatusCatalog.AllStatuses.ToList();
    public bool CanEdit { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public sealed class TechnicianOptionViewModel
{
    public string AssignmentValue { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class PartsCatalogOptionViewModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public sealed class LaborCatalogOptionViewModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DefaultHours { get; set; }
    public decimal HourlyRate { get; set; }
}

using TPGLLC.Data.Entities;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class WorkOrderPageViewModel
{
    public List<ServiceHistoryEntry> WorkOrders { get; set; } = [];
    public List<TechnicianOptionViewModel> TechnicianOptions { get; set; } = [];
    public WorkOrderEditViewModel Form { get; set; } = new();
    public Guid? EditingWorkOrderId { get; set; }
    public List<string> StatusOptions { get; set; } = [
        "Requested",
        "Quoted",
        "Waiting on Customer Approval",
        "Approved",
        "In Progress",
        "Completed",
        "Invoiced",
        "Declined",
        "Cancelled"
    ];
    public bool CanEdit { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public sealed class TechnicianOptionViewModel
{
    public string AssignmentValue { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

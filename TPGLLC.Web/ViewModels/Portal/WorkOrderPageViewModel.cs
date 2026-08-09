using TPGLLC.Data.Entities;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class WorkOrderPageViewModel
{
    public List<ServiceHistoryEntry> WorkOrders { get; set; } = [];
    public WorkOrderEditViewModel Form { get; set; } = new();
    public Guid? EditingWorkOrderId { get; set; }
    public List<string> StatusOptions { get; set; } = [
        "Requested",
        "Quoted",
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

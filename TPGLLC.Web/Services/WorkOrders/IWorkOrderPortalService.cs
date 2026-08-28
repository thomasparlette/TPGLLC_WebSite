using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public interface IWorkOrderPortalService
{
    Task<WorkOrderPageViewModel> GetCustomerAsync();
    Task<WorkOrderPageViewModel> GetStaffWorkOrdersAsync();
    Task<WorkOrderPageViewModel> GetTechnicianWorkOrdersAsync();
    Task<WorkOrderPageViewModel> StartEditAsync(Guid workOrderId);
    Task<WorkOrderPageViewModel> StartTechnicianEditAsync(Guid workOrderId);
    Task<WorkOrderPageViewModel> ResetAsync();
    Task<WorkOrderPageViewModel> SaveAsync(WorkOrderPageViewModel model);
    Task<WorkOrderPageViewModel> SaveTechnicianAsync(WorkOrderPageViewModel model);
    Task<WorkOrderPageViewModel> ApprovePartAsync(Guid workOrderId, Guid partId);
    Task<WorkOrderPageViewModel> DeclinePartAsync(Guid workOrderId, Guid partId);
    Task<WorkOrderPageViewModel> ApproveWorkOrderAsync(Guid workOrderId);
    Task<WorkOrderPageViewModel> DeclineWorkOrderAsync(Guid workOrderId);
}

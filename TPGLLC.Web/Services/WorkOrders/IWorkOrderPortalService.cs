using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public interface IWorkOrderPortalService
{
    Task<WorkOrderPageViewModel> GetCustomerAsync();
    Task<WorkOrderPageViewModel> GetServiceAdvisorAsync();
    Task<WorkOrderPageViewModel> StartEditAsync(Guid workOrderId);
    Task<WorkOrderPageViewModel> ResetAsync();
    Task<WorkOrderPageViewModel> SaveAsync(WorkOrderPageViewModel model);
}

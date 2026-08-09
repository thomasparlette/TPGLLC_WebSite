using TPGLLC.Web.Components.PortalShared.Appointments;
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public interface IWorkOrderPortalService
{
    Task<WorkOrderPageViewModel> GetCustomerAsync();
    Task<WorkOrderPageViewModel> GetEmployeeAsync();
    Task<WorkOrderPageViewModel> StartEditAsync(Guid workOrderId);
    Task<WorkOrderPageViewModel> ResetAsync();
    Task<WorkOrderPageViewModel> SaveAsync(WorkOrderPageViewModel model);

    Task<WorkOrderPageViewModel> ApproveAppointmentAsync(Guid requestId);
    Task<WorkOrderPageViewModel> DeclineAppointmentAsync(Guid requestId);
}
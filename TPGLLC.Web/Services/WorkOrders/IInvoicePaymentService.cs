using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public interface IInvoicePaymentService
{
    Task<InvoicePaymentPageViewModel> GetCompletedWorkOrdersAsync();
    Task<InvoicePaymentPageViewModel> GetPendingPaymentsAsync();
    Task<InvoicePaymentPageViewModel> GetReceivedPaymentsAsync();
    Task<InvoicePaymentPageViewModel> IssueInvoiceAsync(Guid workOrderId);
    Task<InvoicePaymentPageViewModel> RecordPaymentAsync(InvoicePaymentPageViewModel model);
}

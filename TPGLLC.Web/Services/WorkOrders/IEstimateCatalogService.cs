using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.WorkOrders;

public interface IEstimateCatalogService
{
    Task<EstimateCatalogPageViewModel> GetAsync();
    Task<EstimateCatalogPageViewModel> SavePartAsync(EstimateCatalogPageViewModel model);
    Task<EstimateCatalogPageViewModel> SaveLaborAsync(EstimateCatalogPageViewModel model);
}

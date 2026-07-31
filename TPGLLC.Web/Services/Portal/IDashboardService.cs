using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public interface IDashboardService
{
    Task<DashboardViewModel> GetAsync();
}
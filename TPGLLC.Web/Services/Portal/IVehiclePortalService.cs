using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public interface IVehiclePortalService
{
    Task<VehiclePageViewModel> GetAsync();
    Task<VehiclePageViewModel> StartEditAsync(Guid vehicleId);
    Task<VehiclePageViewModel> ResetAsync();
    Task<VehiclePageViewModel> YearChangedAsync(VehiclePageViewModel model);
    Task<VehiclePageViewModel> MakeChangedAsync(VehiclePageViewModel model);
    Task<VehiclePageViewModel> SaveAsync(VehiclePageViewModel model);
    Task<VehiclePageViewModel> DeleteAsync(Guid vehicleId);
    Task<VehiclePageViewModel> MakePrimaryAsync(Guid vehicleId);
}
using TPGLLC.Web.ViewModels.Portal;

namespace TPGLLC.Web.Services.Portal;

public interface IVehicleDetailsService
{
    Task<VehicleDetailsViewModel> GetAsync(Guid vehicleId);
}
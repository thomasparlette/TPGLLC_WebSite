using TPGLLC.Data.Entities;

namespace TPGLLC.Web.Services.Portal;

public interface IPortalContextService
{
    Task<PortalContextViewModel> GetAsync(CancellationToken cancellationToken = default);

    Task<PortalContextViewModel> GetAsync(string userId, CancellationToken cancellationToken = default);

    Task<CustomerVehicle?> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
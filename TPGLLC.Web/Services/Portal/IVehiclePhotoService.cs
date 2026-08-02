using Microsoft.AspNetCore.Components.Forms;

namespace TPGLLC.Web.Services.Portal;

public interface IVehiclePhotoService
{
    Task UploadAsync(Guid vehicleId, IBrowserFile file);
    Task RemoveAsync(Guid vehicleId);
}
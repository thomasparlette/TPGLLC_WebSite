using TPGLLC.Data.Entities;
using TPGLLC.Web.Components.PortalShared.Vehicles;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class VehiclePageViewModel
{
    public List<CustomerVehicle> Vehicles { get; set; } = [];
    public List<int> Years { get; set; } = [];
    public List<string> Makes { get; set; } = [];
    public List<string> Models { get; set; } = [];
    public VehicleFormModel Form { get; set; } = new();
    public Guid? EditingVehicleId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
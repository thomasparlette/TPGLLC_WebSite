using TPGLLC.Data.Entities;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class VehicleDetailsViewModel
{
    public CustomerVehicle? Vehicle { get; set; }

    public List<ServiceHistoryEntry> ServiceHistory { get; set; } = [];

    public string? ErrorMessage { get; set; }

    public string VehicleTitle
    {
        get
        {
            if (Vehicle is null)
            {
                return "Vehicle Details";
            }

            var parts = new[]
            {
                Vehicle.ModelYear?.ToString(),
                Vehicle.Make,
                Vehicle.Model
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            var title = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(title) ? "Unnamed Vehicle" : title;
        }
    }
}
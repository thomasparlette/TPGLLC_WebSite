using TPGLLC.Data.Entities;
using TPGLLC.Web.Components.PortalShared.Appointments;

namespace TPGLLC.Web.ViewModels.Portal;

public sealed class AppointmentPageViewModel
{
    public List<AppointmentRequest> Requests { get; set; } = [];
    public List<AppointmentRequest> OpenRequests { get; set; } = [];

    public List<int> Years { get; set; } = [];
    public List<string> Makes { get; set; } = [];
    public List<string> Models { get; set; } = [];

    public AppointmentRequestFormModel Form { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
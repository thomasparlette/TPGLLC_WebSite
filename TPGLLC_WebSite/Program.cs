using TPGLLC_WebSite.Components;
using TPGLLC_WebSite.Models;
using TPGLLC_WebSite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection("Gmail"));
builder.Services.AddSingleton<IAppointmentRequestStore, FileAppointmentRequestStore>();
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddTransient<IAppointmentRequestService, AppointmentRequestService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.MapPost("/appointment/request", async (HttpRequest request, IAppointmentRequestService appointmentService) =>
{
    var form = await request.ReadFormAsync();

    var appointment = new AppointmentRequest
    {
        Name = form["Name"],
        Phone = form["Phone"],
        Email = form["Email"],
        VehicleType = form["VehicleType"],
        VehicleYear = form["VehicleYear"],
        VehicleMake = form["VehicleMake"],
        VehicleModel = form["VehicleModel"],
        Mileage = form["Mileage"],
        PreferredDate = form["PreferredDate"],
        PreferredTime = form["PreferredTime"],
        ServiceNeeded = form["ServiceNeeded"],
        Message = form["Message"],
        Company = form["Company"]
    };

    if (!string.IsNullOrWhiteSpace(appointment.Company))
    {
        return Results.Redirect("/?appointment=sent");
    }

    if (string.IsNullOrWhiteSpace(appointment.Name) ||
        string.IsNullOrWhiteSpace(appointment.Phone) ||
        string.IsNullOrWhiteSpace(appointment.Email) ||
        string.IsNullOrWhiteSpace(appointment.VehicleYear) ||
        string.IsNullOrWhiteSpace(appointment.VehicleMake) ||
        string.IsNullOrWhiteSpace(appointment.VehicleModel) ||
        string.IsNullOrWhiteSpace(appointment.PreferredDate) ||
        string.IsNullOrWhiteSpace(appointment.PreferredTime) ||
        string.IsNullOrWhiteSpace(appointment.ServiceNeeded) ||
        string.IsNullOrWhiteSpace(appointment.Message))
    {
        return Results.Redirect("/?appointment=missing#appointment");
    }

    var requestId = await appointmentService.SubmitAsync(appointment);
    return Results.Redirect($"/?appointment=sent&requestId={requestId}#appointment");
})
.DisableAntiforgery();
app.Run();
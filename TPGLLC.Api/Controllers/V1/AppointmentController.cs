using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TPGLLC.Data.Entities;
using TPGLLC.Application.Appointments;
using TPGLLC.Services.Scheduling;

namespace TPGLLC.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/appointments")]
public sealed class AppointmentController : ControllerBase
{
    private readonly IAppointmentRequestService _appointments;

    public AppointmentController(IAppointmentRequestService appointments)
    {
        _appointments = appointments;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Company))
        {
            return Accepted();
        }

        var domainRequest = new AppointmentRequest
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            VehicleType = request.VehicleType,
            VehicleYear = request.VehicleYear,
            VehicleMake = request.VehicleMake,
            VehicleModel = request.VehicleModel,
            Vin = request.Vin,
            Mileage = request.Mileage,
            PreferredDate = request.PreferredDate,
            PreferredTime = request.PreferredTime,
            ServiceNeeded = request.ServiceNeeded,
            Message = request.Message,
            Company = request.Company
        };

        var requestId = await _appointments.SubmitAsync(domainRequest, cancellationToken);

        var response = new CreateAppointmentResponse(
            requestId,
            domainRequest.Status,
            domainRequest.SubmittedAtUtc);

        return Created($"/api/v1/appointments/{requestId}", response);
    }
}
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TPGLLC.Application.Vehicles;
using TPGLLC.Services.Vehicles;

namespace TPGLLC.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vehicles")]
public sealed class VehicleController : ControllerBase
{
    private readonly IVehicleCatalogService _catalog;

    public VehicleController(IVehicleCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<VehicleTypeDto>>> GetTypes(CancellationToken cancellationToken)
    {
        var result = await _catalog.GetVehicleTypesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("years")]
    public async Task<ActionResult<IReadOnlyList<VehicleYearDto>>> GetYears(
        [FromQuery] string vehicleType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vehicleType))
        {
            return BadRequest(new { message = "vehicleType is required." });
        }

        var result = await _catalog.GetYearsAsync(vehicleType, cancellationToken);
        return Ok(result);
    }

    [HttpGet("makes")]
    public async Task<ActionResult<IReadOnlyList<VehicleMakeDto>>> GetMakes(
        [FromQuery] string vehicleType,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vehicleType))
        {
            return BadRequest(new { message = "vehicleType is required." });
        }

        var result = await _catalog.GetMakesAsync(vehicleType, year, cancellationToken);
        return Ok(result);
    }

    [HttpGet("models")]
    public async Task<ActionResult<IReadOnlyList<VehicleModelDto>>> GetModels(
        [FromQuery] string vehicleType,
        [FromQuery] int year,
        [FromQuery] string make,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(vehicleType) || string.IsNullOrWhiteSpace(make))
        {
            return BadRequest(new { message = "vehicleType and make are required." });
        }

        var result = await _catalog.GetModelsAsync(vehicleType, year, make, cancellationToken);
        return Ok(result);
    }
}
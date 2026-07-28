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
 
    [HttpGet("years")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetYears(CancellationToken cancellationToken)
    {
        var result = await _catalog.GetYearsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("makes")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetMakes(
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        if (year <= 0)
        {
            return BadRequest(new { message = "year is required." });
        }

        var result = await _catalog.GetMakesAsync(year, cancellationToken);
        return Ok(result);
    }

    [HttpGet("models")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetModels(
        [FromQuery] int year,
        [FromQuery] string make,
        CancellationToken cancellationToken)
    {
        if (year <= 0 || string.IsNullOrWhiteSpace(make))
        {
            return BadRequest(new { message = "year and make are required." });
        }

        var result = await _catalog.GetModelsAsync(year, make, cancellationToken);
        return Ok(result);
    }
}
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace TPGLLC.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "TPGLLC.Api",
        version = "v1"
    });
}
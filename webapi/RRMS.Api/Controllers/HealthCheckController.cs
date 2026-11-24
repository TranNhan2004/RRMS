using Microsoft.AspNetCore.Mvc;

namespace RRMS.Api.Controllers;

[Route("api/health-check")]
[ApiController]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public IActionResult Healthy()
    {
        return Ok("Healthy");
    }
}
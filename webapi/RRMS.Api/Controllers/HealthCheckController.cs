using Microsoft.AspNetCore.RateLimiting;
using RRMS.Domain.Constants.Policies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace RRMS.Api.Controllers;

[EnableRateLimiting(RateLimiterPolicies.Standard)]
[Route("api/health-check")]
[ApiController]
[AllowAnonymous]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public IActionResult Healthy()
    {
        return Ok("Healthy");
    }
}
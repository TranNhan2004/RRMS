using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RRMS.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class GoogleAuthenticationController : ControllerBase
{
    [HttpGet("oauth2/authorization/google")]
    public async Task<IActionResult> GoogleAsync([FromServices] IAuthenticationSchemeProvider schemes)
    {
        if (await schemes.GetSchemeAsync("Google") == null) return NotFound();
        return Challenge(new AuthenticationProperties(), "Google");
    }
}

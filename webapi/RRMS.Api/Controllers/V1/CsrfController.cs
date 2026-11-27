using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRMS.Infrastructure.Services.Csrf;

namespace RRMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/csrf")]
public sealed class CsrfController : ControllerBase
{
    private readonly CsrfService _csrfService;

    public CsrfController(CsrfService csrfService)
    {
        _csrfService = csrfService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetToken()
    {
        _csrfService.IssueToken(HttpContext);
        return NoContent();
    }
}

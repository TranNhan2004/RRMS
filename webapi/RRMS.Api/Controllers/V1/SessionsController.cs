using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRMS.Api.Dtos.V1.Authentication;
using RRMS.Application.Interfaces.Services.Authentication;

namespace RRMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/auth")]
public class SessionsController : ControllerBase
{
    private readonly ISessionManagementService _sessionManagementService;

    public SessionsController(ISessionManagementService sessionManagementService)
    {
        _sessionManagementService = sessionManagementService;
    }

    [HttpPost("switch-role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SwitchRoleAsync([FromBody] SelectRoleRequest request, CancellationToken cancellationToken)
    {
        await _sessionManagementService.SwitchRoleAsync(request.RoleCode, cancellationToken);
        return NoContent();
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        await _sessionManagementService.LogoutAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAllAsync(CancellationToken cancellationToken)
    {
        await _sessionManagementService.LogoutAllAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<SessionResponse>))]
    public async Task<IActionResult> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var models = await _sessionManagementService.GetSessionsAsync(cancellationToken);
        var dtoResponse = models.Select(SessionResponse.FromModel).ToArray();
        return Ok(dtoResponse);
    }

    [HttpDelete("sessions/{reference}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSessionAsync(string reference, CancellationToken cancellationToken)
    {
        return await _sessionManagementService.RevokeSessionAsync(reference, cancellationToken) ? NoContent() : NotFound();
    }
}

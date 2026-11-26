using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRMS.Api.Dtos.V1.Authentication;
using RRMS.Domain.Constants.Authentication;
using RRMS.Infrastructure.Services.Authentication;

namespace RRMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth/google")]
public class GoogleAuthenticationController : ControllerBase
{
    private readonly GoogleAuthenticationService _googleAuthenticationService;

    public GoogleAuthenticationController(GoogleAuthenticationService googleAuthenticationService)
    {
        _googleAuthenticationService = googleAuthenticationService;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginAsync(CancellationToken cancellationToken)
    {
        var properties = await _googleAuthenticationService.PrepareLoginAsync(cancellationToken);
        return Challenge(properties, AuthenticationSchemes.Google);
    }

    [Authorize]
    [HttpPost("link")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GoogleLinkResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LinkAsync(CancellationToken cancellationToken)
    {
        var result = await _googleAuthenticationService.BeginLinkAsync(cancellationToken);
        if (result == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        var dtoResponse = GoogleLinkResponse.FromModel(result);
        return Ok(dtoResponse);
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CallbackAsync(CancellationToken cancellationToken)
    {
        var destination = await _googleAuthenticationService.CompleteCallbackAsync(cancellationToken);
        return Redirect(destination);
    }
}

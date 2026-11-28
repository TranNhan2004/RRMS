using Microsoft.AspNetCore.RateLimiting;
using RRMS.Domain.Constants.Policies;
using RRMS.Domain.Constants.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRMS.Infrastructure.Services.Authentication;
using RRMS.Api.Dtos.V1.Authentication;
using RRMS.Application.Interfaces.Services.Authentication;
using IAuthenticationService = RRMS.Application.Interfaces.Services.Authentication.IAuthenticationService;

namespace RRMS.Api.Controllers.V1;

[EnableRateLimiting(RateLimiterPolicies.Auth)]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IRegistrationService _registrationService;
    private readonly AuthenticationSessionService _authenticationSessionService;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        IRegistrationService registrationService,
        AuthenticationSessionService authenticationSessionService)
    {
        _authenticationService = authenticationService;
        _registrationService = registrationService;
        _authenticationSessionService = authenticationSessionService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(RegistrationResponse))]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(RegistrationResponse))]
    public async Task<IActionResult> RegisterAsync([FromBody] RegistrationRequest request, CancellationToken cancellationToken)
    {
        var model = RegistrationRequest.ToModel(request);
        var result = await _registrationService.RegisterAsync(model, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        var dtoResponse = RegistrationResponse.FromModel(result);
        var statusCode = request.Stage == RegistrationStages.Initiate
            ? StatusCodes.Status202Accepted
            : StatusCodes.Status201Created;
        return StatusCode(statusCode, dtoResponse);
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VerifyEmailResponse))]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var model = VerifyEmailRequest.ToModel(request);
        var result = await _registrationService.VerifyEmailAsync(model, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        var dtoResponse = VerifyEmailResponse.FromModel(result);
        return Ok(dtoResponse);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var model = LoginRequest.ToModel(request);
        var result = await _authenticationService.LoginAsync(model, cancellationToken);
        await _authenticationSessionService.CompleteAsync(result, cancellationToken);
        var dtoResponse = LoginResponse.FromModel(result);
        return Ok(dtoResponse);
    }

    [AllowAnonymous]
    [HttpGet("pending-session")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    public async Task<IActionResult> GetPendingSessionAsync(CancellationToken cancellationToken)
    {
        var result = await _authenticationService.GetPendingSessionAsync(cancellationToken);
        Response.Headers.CacheControl = "no-store";
        var dtoResponse = LoginResponse.FromModel(result);
        return Ok(dtoResponse);
    }

    [AllowAnonymous]
    [HttpPost("select-role")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    public async Task<IActionResult> SelectRoleAsync([FromBody] SelectRoleRequest request, CancellationToken cancellationToken)
    {
        var model = SelectRoleRequest.ToModel(request);
        var result = await _authenticationService.SelectRoleAsync(model, cancellationToken);
        await _authenticationSessionService.CompleteAsync(result, cancellationToken);
        var dtoResponse = LoginResponse.FromModel(result);
        return Ok(dtoResponse);
    }

}

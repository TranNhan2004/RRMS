using Microsoft.AspNetCore.RateLimiting;
using RRMS.Domain.Constants.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRMS.Api.Dtos.Common;
using RRMS.Api.Dtos.V1.Users;
using RRMS.Application.Interfaces.Services.Users;

namespace RRMS.Api.Controllers.V1;

[EnableRateLimiting(RateLimiterPolicies.Standard)]
[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserQueryService _userQueryService;
    private readonly IUserCommandService _userCommandService;

    public UserController(IUserQueryService userQueryService, IUserCommandService userCommandService)
    {
        _userQueryService = userQueryService;
        _userCommandService = userCommandService;
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetCurrentUserResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var result = await _userQueryService.GetCurrentUserAsync(cancellationToken);
        var dtoResponse = GetCurrentUserResponse.FromModel(result);
        return Ok(dtoResponse);
    }
    [HttpPut("me/user-name")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SetUserNameResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> SetUserNameAsync([FromBody] SetUserNameRequest request, CancellationToken cancellationToken = default)
    {
        var model = SetUserNameRequest.ToModel(request);
        var result = await _userCommandService.SetUserNameAsync(model, cancellationToken);
        var dtoResponse = SetUserNameResponse.FromModel(result);
        return Ok(dtoResponse);
    }
}

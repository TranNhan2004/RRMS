using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRMS.Api.Dtos.Common;
using RRMS.Api.Dtos.V1.Users;
using RRMS.Application.Interfaces.Services.Users;

namespace RRMS.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserQueryService _userQueryService;

    public UserController(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
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
}

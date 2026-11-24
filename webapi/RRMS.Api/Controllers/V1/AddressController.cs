using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RRMS.Api.Dtos.Common;
using RRMS.Api.Dtos.V1.Addresses;
using RRMS.Application.Interfaces.Services.Addresses;

namespace RRMS.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/addresses")]
[ApiVersion("1.0")]
[ApiController]
[AllowAnonymous]
public class AddressController : ControllerBase
{
    private readonly IAddressQueryService _addressQueryService;

    public AddressController(IAddressQueryService addressQueryService)
    {
        _addressQueryService = addressQueryService;
    }

    [HttpPost("provinces")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetFilteredProvincesResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetFilteredProvincesAsync(
        [FromBody] GetFilteredProvincesRequest request,
        CancellationToken cancellationToken = default)
    {
        var model = GetFilteredProvincesRequest.ToModel(request);
        var result = await _addressQueryService.GetFilteredProvincesAsync(model, cancellationToken);
        var dtoResponse = GetFilteredProvincesResponse.FromModel(result);
        return Ok(dtoResponse);
    }

    [HttpPost("wards")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetFilteredWardsResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetFilteredWardsAsync(
        [FromBody] GetFilteredWardsRequest request,
        CancellationToken cancellationToken = default)
    {
        var model = GetFilteredWardsRequest.ToModel(request);
        var result = await _addressQueryService.GetFilteredWardsAsync(model, cancellationToken);
        var dtoResponse = GetFilteredWardsResponse.FromModel(result);
        return Ok(dtoResponse);
    }
}

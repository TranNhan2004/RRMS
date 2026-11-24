using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RRMS.Api.Dtos.V1.Authentication;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Infrastructure.Options;

namespace RRMS.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
[ApiController]
[AllowAnonymous]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authentication;
    private readonly CredentialAuthOptions _options;
    private readonly CsrfOptions _csrf;

    public AuthenticationController(IAuthenticationService authentication, IOptions<AuthOptions> options, IOptions<CsrfOptions> csrf)
    {
        _authentication = authentication;
        _options = options.Value.Credentials;
        _csrf = csrf.Value;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(RegistrationResponse))]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(RegistrationResponse))]
    public async Task<IActionResult> RegisterAsync([FromBody] RegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var model = RegistrationRequest.ToModel(request, RemoteAddress());
        var result = await _authentication.RegisterAsync(model, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return StatusCode(request.Stage == "INITIATE" ? StatusCodes.Status202Accepted : StatusCodes.Status201Created, RegistrationResponse.FromModel(result));
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VerifyEmailResponse))]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _authentication.VerifyEmailAsync(VerifyEmailRequest.ToModel(request), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(VerifyEmailResponse.FromModel(result));
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var model = LoginRequest.ToModel(request, RemoteAddress(), Request.Cookies["RRMS_ROLE_SELECTION"]);
        var result = await _authentication.LoginAsync(model, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        SetAuthenticationCookies(result);
        return Ok(LoginResponse.FromModel(result));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RefreshResponse))]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var result = await _authentication.RefreshAsync(RefreshRequest.ToModel(Request.Cookies["RRMS_REFRESH"]), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(RefreshResponse.FromModel(result));
    }

    private void SetAuthenticationCookies(AuthenticationResultResponseModel result)
    {
        if (result.RefreshToken == null || result.CsrfToken == null) return;
        Response.Cookies.Append("RRMS_REFRESH", result.RefreshToken, Cookie(true, _options.RefreshCookiePath, _options.RefreshToken.Expiration));
        Response.Cookies.Append(_csrf.CookieName, result.CsrfToken, Cookie(false, "/", _options.RefreshToken.Expiration));
        if (Request.Cookies.ContainsKey("RRMS_ROLE_SELECTION"))
            Response.Cookies.Append("RRMS_ROLE_SELECTION", "", Cookie(true, "/api/v1/auth/login", TimeSpan.Zero));
    }

    private CookieOptions Cookie(bool httpOnly, string path, TimeSpan lifetime) => new()
    {
        HttpOnly = httpOnly,
        Secure = _options.SecureCookies,
        SameSite = Enum.TryParse<SameSiteMode>(_options.SameSite, true, out var sameSite) ? sameSite : SameSiteMode.Strict,
        Path = path,
        MaxAge = lifetime
    };

    private string RemoteAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

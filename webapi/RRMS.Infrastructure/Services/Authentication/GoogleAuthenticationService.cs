using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Exceptions;
using RRMS.Domain.Constants.Authentication;
using RRMS.Infrastructure.Options;
using IAuthenticationService = RRMS.Application.Interfaces.Services.Authentication.IAuthenticationService;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Coordinates Google sign-in, account-linking challenges, and OAuth callback handling.
/// It validates the initiating session for linking and clears the temporary external session.
/// </summary>
public sealed class GoogleAuthenticationService
{
    private const string CallbackPath = "/api/v1/auth/google/callback";
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IAuthenticationChallengeStore _challenges;
    private readonly IAuthenticationService _authenticationService;
    private readonly IGoogleIdentityCommandService _googleIdentityCommandService;
    private readonly AuthenticationSessionService _authenticationSessionService;
    private readonly AuthenticationEventLogger _eventLogger;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthOptions _options;

    public GoogleAuthenticationService(
        IAuthenticationSchemeProvider schemes,
        IAuthenticationChallengeStore challenges,
        IAuthenticationService authenticationService,
        IGoogleIdentityCommandService googleIdentityCommandService,
        AuthenticationSessionService authenticationSessionService,
        AuthenticationEventLogger eventLogger,
        ICurrentUserContext currentUserContext,
        ICurrentRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuthOptions> options)
    {
        _schemes = schemes;
        _challenges = challenges;
        _authenticationService = authenticationService;
        _googleIdentityCommandService = googleIdentityCommandService;
        _authenticationSessionService = authenticationSessionService;
        _eventLogger = eventLogger;
        _currentUserContext = currentUserContext;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    private HttpContext Context => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP request.");

    public async Task<AuthenticationProperties> PrepareLoginAsync(CancellationToken cancellationToken)
    {
        if (_currentUserContext.IsAuthenticated)
        {
            throw new ConflictException("User is already authenticated.");
        }
        await EnsureSchemeAsync();
        if (!await _challenges.AllowAsync("google-login-ip", _requestContext.RemoteIpAddress ?? "unknown", 60, TimeSpan.FromMinutes(15), cancellationToken))
        {
            throw new RateLimitException("Too many authentication attempts. Try again later.");
        }
        return new AuthenticationProperties
        {
            RedirectUri = CallbackPath
        };
    }

    public async Task<GoogleLinkResponseModel?> BeginLinkAsync(CancellationToken cancellationToken)
    {
        await EnsureSchemeAsync();
        var sessionReference = _currentUserContext.SessionReference;
        if (string.IsNullOrEmpty(sessionReference))
        {
            throw new UnauthorizedException("An active session is required.");
        }
        var userId = _currentUserContext.GetRequiredUserId();
        if (!await _challenges.AllowAsync("google-link-user", userId.ToString(), 10, TimeSpan.FromMinutes(15), cancellationToken))
        {
            throw new RateLimitException("Too many authentication attempts. Try again later.");
        }
        var properties = new AuthenticationProperties
        {
            RedirectUri = CallbackPath
        };
        properties.Items["linkUserId"] = userId.ToString();
        properties.Items["linkSessionRef"] = sessionReference;
        await Context.ChallengeAsync(AuthenticationSchemes.Google, properties);
        var authorizationUrl = Context.Response.Headers.Location.ToString();
        Context.Response.Headers.Remove("Location");
        Context.Response.Headers.CacheControl = "no-store";
        Context.Response.StatusCode = StatusCodes.Status200OK;
        return string.IsNullOrEmpty(authorizationUrl) ? null : new GoogleLinkResponseModel
        {
            AuthorizationUrl = authorizationUrl
        };
    }

    public async Task<string> CompleteCallbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            var external = await Context.AuthenticateAsync(AuthenticationSchemes.External);
            if (!external.Succeeded || external.Principal == null)
            {
                throw new UnauthorizedException("External authentication failed.");
            }
            var subject = external.Principal.FindFirstValue(AuthenticationClaimTypes.Subject);
            if (string.IsNullOrEmpty(subject))
            {
                throw new UnauthorizedException("External identity is missing.");
            }
            var model = new GoogleLoginRequestModel
            {
                Subject = subject,
                Email = external.Principal.FindFirstValue("email"),
                EmailVerified = bool.TryParse(external.Principal.FindFirstValue("email_verified"), out var verified) && verified
            };
            if (external.Properties?.Items.TryGetValue("linkUserId", out var linkUser) == true)
            {
                var sessionReference = _currentUserContext.SessionReference;
                if (!_currentUserContext.IsAuthenticated || string.IsNullOrEmpty(sessionReference) ||
                    linkUser != _currentUserContext.GetRequiredUserId().ToString() ||
                    !external.Properties.Items.TryGetValue("linkSessionRef", out var linkSession) ||
                    linkSession != sessionReference)
                {
                    throw new UnauthorizedException("The linking session is no longer valid.");
                }
                var userId = _currentUserContext.GetRequiredUserId();
                await _googleIdentityCommandService.LinkAsync(userId, model.Subject, model.Email, model.EmailVerified, cancellationToken);
                _eventLogger.Log("GoogleIdentityLinked", userId, _currentUserContext.ActiveRole, sessionReference);
            }
            else
            {
                if (_currentUserContext.IsAuthenticated)
                {
                    throw new ConflictException("User is already authenticated.");
                }
                var result = await _authenticationService.GoogleLoginAsync(model, cancellationToken);
                await _authenticationSessionService.CompleteAsync(result, cancellationToken);
            }
            return _options.FrontendPath;
        }
        finally
        {
            await Context.SignOutAsync(AuthenticationSchemes.External);
        }
    }

    private async Task EnsureSchemeAsync()
    {
        if (await _schemes.GetSchemeAsync(AuthenticationSchemes.Google) == null)
        {
            throw new NotFoundException("Google authentication is unavailable.");
        }
    }
}

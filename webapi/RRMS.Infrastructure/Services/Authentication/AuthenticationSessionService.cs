using Microsoft.AspNetCore.Http;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Constants.Authentication;
using System.Security.Claims;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Completes an authentication result by creating a signed-in or pending session.
/// It coordinates session cookies and records the completed authentication event.
/// </summary>
public sealed class AuthenticationSessionService
{
    private readonly SessionCookieService _sessionCookies;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationEventLogger _eventLogger;
    private string? _pendingToken;

    public AuthenticationSessionService(SessionCookieService sessionCookies, IHttpContextAccessor httpContextAccessor, AuthenticationEventLogger eventLogger)
    {
        _sessionCookies = sessionCookies;
        _httpContextAccessor = httpContextAccessor;
        _eventLogger = eventLogger;
    }

    internal void SetPendingToken(string token) => _pendingToken = token;

    public async Task CompleteAsync(AuthenticationResultResponseModel result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP request.");
        context.Response.Headers.CacheControl = "no-store";
        switch (result.State)
        {
            case AuthenticationStates.Authenticated:
                var wasPending = !string.IsNullOrEmpty(_sessionCookies.ReadPending(context));
                await _sessionCookies.SignInAsync(context, result);
                _sessionCookies.ClearPending(context);
                var authenticatedEvent = wasPending ? "RoleSelected" : result.AuthenticationMethod == "google" ? "GoogleLoginSucceeded" : "LoginSucceeded";
                _eventLogger.Log(authenticatedEvent, result.UserId, result.ActiveRole, context.User.FindFirstValue(AuthenticationClaimTypes.SessionReference));
                break;
            case AuthenticationStates.RoleSelectionRequired:
                if (string.IsNullOrEmpty(_pendingToken))
                {
                    throw new InvalidOperationException("Pending authentication token is missing.");
                }
                _sessionCookies.WritePending(context, _pendingToken);
                _pendingToken = null;
                _eventLogger.Log(result.AuthenticationMethod == "google" ? "PendingGoogleLoginCreated" : "PendingLoginCreated", result.UserId, result.ActiveRole);
                break;
            default:
                throw new InvalidOperationException("Unsupported authentication state.");
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Constants.Authentication;
using RRMS.Domain.Exceptions;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Manages the current user’s sessions and active role.
/// It checks role eligibility, coordinates session rotation or revocation, and records the resulting events.
/// </summary>
public class SessionManagementService : ISessionManagementService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthenticationDataService _authenticationDataService;
    private readonly RedisTicketStore _redisTicketStore;
    private readonly SessionCookieService _sessionCookies;
    private readonly AuthenticationEventLogger _eventLogger;

    public SessionManagementService(
        ICurrentUserContext currentUserContext,
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationDataService authenticationDataService,
        RedisTicketStore redisTicketStore,
        SessionCookieService sessionCookies,
        AuthenticationEventLogger eventLogger)
    {
        _currentUserContext = currentUserContext;
        _httpContextAccessor = httpContextAccessor;
        _authenticationDataService = authenticationDataService;
        _redisTicketStore = redisTicketStore;
        _sessionCookies = sessionCookies;
        _eventLogger = eventLogger;
    }

    private HttpContext Context => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP request.");

    public async Task<IReadOnlyList<SessionResponseModel>> GetSessionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tickets = await _redisTicketStore.FindUserSessionsAsync(_currentUserContext.GetRequiredUserId());
        Context.Response.Headers.CacheControl = "no-store";
        return tickets.Select(ticket => new SessionResponseModel
        {
            Reference = ticket.Principal.FindFirstValue(AuthenticationClaimTypes.SessionReference)!,
            ActiveRole = ticket.Principal.FindFirstValue(AuthenticationClaimTypes.ActiveRole),
            IssuedAt = ticket.Properties.IssuedUtc,
            ExpiresAt = ticket.Properties.ExpiresUtc
        }).ToArray();
    }

    public async Task<bool> RevokeSessionAsync(string reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var revoked = await _redisTicketStore.RemoveReferenceAsync(_currentUserContext.GetRequiredUserId(), reference);
        if (revoked)
        {
            var userId = _currentUserContext.GetRequiredUserId();
            var role = _currentUserContext.ActiveRole;
            if (reference == _currentUserContext.SessionReference)
            {
                await Context.SignOutAsync(AuthenticationSchemes.Session);
            }
            _eventLogger.Log("SessionRevoked", userId, role, reference);
        }
        return revoked;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUserContext.GetRequiredUserId();
        var role = _currentUserContext.ActiveRole;
        var sessionReference = _currentUserContext.SessionReference;
        await Context.SignOutAsync(AuthenticationSchemes.Session);
        _eventLogger.Log("Logout", userId, role, sessionReference);
    }

    public async Task LogoutAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUserContext.GetRequiredUserId();
        var role = _currentUserContext.ActiveRole;
        var sessionReference = _currentUserContext.SessionReference;
        await _redisTicketStore.RemoveAllAsync(userId);
        await Context.SignOutAsync(AuthenticationSchemes.Session);
        _eventLogger.Log("LogoutAll", userId, role, sessionReference);
    }

    public async Task SwitchRoleAsync(string roleCode, CancellationToken cancellationToken)
    {
        var userId = _currentUserContext.GetRequiredUserId();
        var roles = await _authenticationDataService.FindEligibleRolesAsync(userId, cancellationToken);
        if (!roles.Contains(roleCode, StringComparer.Ordinal))
        {
            throw new ForbiddenException("The requested role is not available.");
        }
        if (_currentUserContext.ActiveRole == roleCode)
        {
            return;
        }
        if (!await _sessionCookies.RotateAsync(Context, roleCode))
        {
            throw new UnauthorizedException("The session is no longer active.");
        }
        _eventLogger.Log("ActiveRoleSwitched", userId, roleCode, Context.User.FindFirstValue(AuthenticationClaimTypes.SessionReference));
    }
}

using RRMS.Domain.Constants.Authentication;
using Microsoft.AspNetCore.Http;
using RRMS.Application.Interfaces.Contexts;
using Microsoft.Extensions.Options;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Validates credential and Google logins and manages pending role selection.
/// It returns an authentication result for the session service to complete.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private const string DummyPasswordHash = "{argon2}$argon2id$v=19$m=19456,t=2,p=1$MDEyMzQ1Njc4OWFiY2RlZg$9CWXp0XaAuMQPZzNccREnUDxhapXcY1ekEZi8T8qqxw";
    private readonly IAuthenticationChallengeStore _challenges;
    private readonly IAuthenticationDataService _data;
    private readonly IPasswordService _passwords;
    private readonly AuthOptions _authOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SessionCookieService _sessionCookieService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ICurrentRequestContext _currentRequestContext;
    private readonly AuthenticationSessionService _authenticationSessionService;
    private readonly AuthenticationEventLogger _eventLogger;

    public AuthenticationService(
        IAuthenticationChallengeStore challenges,
        IAuthenticationDataService data,
        IPasswordService passwords,
        IOptions<AuthOptions> options,
        IHttpContextAccessor httpContextAccessor,
        SessionCookieService sessionCookieService,
        ICurrentUserContext currentUserContext,
        ICurrentRequestContext currentRequestContext,
        AuthenticationSessionService authenticationSessionService,
        AuthenticationEventLogger eventLogger)
    {
        _challenges = challenges;
        _data = data;
        _passwords = passwords;
        _authOptions = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _sessionCookieService = sessionCookieService;
        _currentUserContext = currentUserContext;
        _currentRequestContext = currentRequestContext;
        _authenticationSessionService = authenticationSessionService;
        _eventLogger = eventLogger;
    }

    private string? PendingToken => _httpContextAccessor.HttpContext is { } context ? _sessionCookieService.ReadPending(context) : null;

    public async Task<AuthenticationResultResponseModel> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default)
    {
        if (_currentUserContext.IsAuthenticated)
        {
            throw new ConflictException("User is already authenticated.");
        }
        try
        {
            if (string.IsNullOrWhiteSpace(request.Identity) || request.Identity.Length > 255 || string.IsNullOrEmpty(request.Password) || request.Password.Length > 256)
            {
                throw InvalidCredentials();
            }

            var identity = request.Identity.Trim().ToLowerInvariant();
            if (!await _challenges.AllowAsync("login-ip", _currentRequestContext.RemoteIpAddress ?? "unknown", 60, TimeSpan.FromMinutes(15), cancellationToken) ||
                !await _challenges.AllowAsync("login-account", identity, 10, TimeSpan.FromMinutes(15), cancellationToken))
            {
                throw new RateLimitException("Too many authentication attempts. Try again later.");
            }

            var matches = await _data.FindCredentialsAsync(identity, cancellationToken);
            var passwordMatches = _passwords.Verify(request.Password, matches.Count == 1 ? matches[0].PasswordHash : DummyPasswordHash);
            if (matches.Count != 1 || !matches[0].User.IsEmailVerified || matches[0].User.Status != UserStatus.Active || !passwordMatches)
            {
                throw InvalidCredentials();
            }

            return await BeginAsync(matches[0].UserId, "password", cancellationToken);
        }
        catch (Exception exception) when (exception is UnauthorizedException or RateLimitException)
        {
            _eventLogger.Log("LoginFailed", null, null);
            throw;
        }
    }

    public async Task<AuthenticationResultResponseModel> GetPendingSessionAsync(CancellationToken cancellationToken = default)
    {
        var userId = ParseChallenge(await _challenges.FindAsync("role-selection", PendingToken, cancellationToken));
        var roles = await EligibleRolesAsync(userId, cancellationToken);
        var user = await FindActiveUserAsync(userId, cancellationToken);
        var remaining = await _challenges.FindRemainingLifetimeAsync("role-selection", PendingToken, cancellationToken);
        if (remaining == null || remaining <= TimeSpan.Zero)
        {
            throw InvalidCredentials();
        }

        return new AuthenticationResultResponseModel
        {
            State = AuthenticationStates.RoleSelectionRequired,
            Roles = roles,
            UserId = userId,
            UserName = user.UserName,
            PendingExpiresAt = DateTimeOffset.UtcNow.Add(remaining.Value)
        };
    }

    public async Task<AuthenticationResultResponseModel> SelectRoleAsync(SelectRoleRequestModel request, CancellationToken cancellationToken = default)
    {
        if (_currentUserContext.IsAuthenticated)
        {
            throw new ConflictException("User is already authenticated.");
        }
        var challenge = await _challenges.FindAsync("role-selection", PendingToken, cancellationToken);
        var userId = ParseChallenge(challenge);
        var roles = await EligibleRolesAsync(userId, cancellationToken);
        if (request.RoleCode == null || !roles.Contains(request.RoleCode, StringComparer.Ordinal))
        {
            throw InvalidCredentials();
        }

        await FindActiveUserAsync(userId, cancellationToken);
        var consumed = await _challenges.ConsumeAsync("role-selection", PendingToken, cancellationToken);
        if (consumed != challenge || !(await EligibleRolesAsync(userId, cancellationToken)).Contains(request.RoleCode, StringComparer.Ordinal))
        {
            throw InvalidCredentials();
        }

        return await IssueAsync(userId, request.RoleCode, challenge!.Split(':')[1], cancellationToken);
    }

    public async Task<AuthenticationResultResponseModel> GoogleLoginAsync(GoogleLoginRequestModel request, CancellationToken cancellationToken = default)
    {
        var id = await _data.ResolveGoogleUserAsync(request.Subject, request.Email, request.EmailVerified, cancellationToken);
        return await BeginAsync(id, "google", cancellationToken);
    }

    private async Task<AuthenticationResultResponseModel> BeginAsync(Guid userId, string method, CancellationToken cancellationToken)
    {
        var roles = await EligibleRolesAsync(userId, cancellationToken);
        if (roles.Count == 1)
        {
            return await IssueAsync(userId, roles[0], method, cancellationToken);
        }

        var challenge = await _challenges.IssueAsync("role-selection", $"{userId}:{method}", _authOptions.Credentials.RoleSelectionTtl, cancellationToken);
        _authenticationSessionService.SetPendingToken(challenge);
        return new AuthenticationResultResponseModel
        {
            State = AuthenticationStates.RoleSelectionRequired,
            Roles = roles,
            UserId = userId,
            AuthenticationMethod = method
        };
    }

    private async Task<IReadOnlyList<string>> EligibleRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await _data.FindEligibleRolesAsync(userId, cancellationToken);
        return roles.Count == 0 ? throw InvalidCredentials() : roles;
    }

    private async Task<AuthenticationResultResponseModel> IssueAsync(Guid userId, string role, string method, CancellationToken cancellationToken)
    {
        var user = await FindActiveUserAsync(userId, cancellationToken);

        return new AuthenticationResultResponseModel
        {
            State = AuthenticationStates.Authenticated,
            UserId = userId,
            UserName = user.UserName,
            Email = user.Email,
            ActiveRole = role,
            AuthenticationMethod = method
        };
    }

    private async Task<RRMS.Domain.Entities.User> FindActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _data.FindUserAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted || !user.IsEmailVerified || user.Status != UserStatus.Active)
        {
            throw InvalidCredentials();
        }
        return user;
    }

    private static Guid ParseChallenge(string? challenge)
    {
        var parts = challenge?.Split(':');
        return parts is { Length: 2 } && (parts[1] == "password" || parts[1] == "google") && Guid.TryParse(parts[0], out var id)
            ? id : throw InvalidCredentials();
    }

    private static UnauthorizedException InvalidCredentials() => new("Invalid authentication credentials");
}

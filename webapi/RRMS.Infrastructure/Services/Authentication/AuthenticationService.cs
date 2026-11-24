using System.Net.Mail;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private const string DummyPasswordHash = "{argon2}$argon2id$v=19$m=19456,t=2,p=1$MDEyMzQ1Njc4OWFiY2RlZg$9CWXp0XaAuMQPZzNccREnUDxhapXcY1ekEZi8T8qqxw";
    private readonly IAuthenticationChallengeStore _challenges;
    private readonly IAuthenticationDataService _data;
    private readonly IAuthenticationEmailService _email;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly CredentialAuthOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly Serilog.ILogger _logger;

    public AuthenticationService(IAuthenticationChallengeStore challenges, IAuthenticationDataService data,
        IAuthenticationEmailService email, IPasswordService passwords, ITokenService tokens,
        IOptions<AuthOptions> options, IHostEnvironment environment, Serilog.ILogger logger)
    {
        _challenges = challenges;
        _data = data;
        _email = email;
        _passwords = passwords;
        _tokens = tokens;
        _options = options.Value.Credentials;
        _environment = environment;
        _logger = logger;
    }

    public async Task<RegistrationResponseModel> RegisterAsync(RegistrationRequestModel request, CancellationToken cancellationToken = default)
    {
        if (request.Stage == "INITIATE")
        {
            var email = NormalizeEmail(request.Email);
            _email.ValidateConfiguration();
            if (!Uri.TryCreate(_options.VerificationUrl, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
                !(uri.Scheme == Uri.UriSchemeHttps || (_environment.IsEnvironment("Local") && uri.Scheme == Uri.UriSchemeHttp)))
                throw new InvalidOperationException("Invalid verification URL configuration.");
            if (await _challenges.AllowAsync("register-ip", request.RemoteAddress, 20, TimeSpan.FromHours(1), cancellationToken) &&
                await _challenges.AllowAsync("register-email", email, 3, TimeSpan.FromHours(1), cancellationToken) &&
                !await _data.FindEmailExistsAsync(email, cancellationToken))
            {
                var token = await _challenges.IssueAsync("registration-verify", email, _options.RegistrationVerificationTtl, cancellationToken);
                try
                {
                    await _email.SendVerificationAsync(email, QueryHelpers.AddQueryString(uri.ToString(), "token", token), cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    await _challenges.ConsumeAsync("registration-verify", token, cancellationToken);
                    _logger.Warning("Verification email delivery failed");
                }
            }
            return new RegistrationResponseModel { State = "REGISTRATION_PENDING" };
        }
        if (request.Stage == "COMPLETE")
        {
            if (request.Password is null || request.Password.Length < 12 || request.Password.Length > 256)
                throw new UnprocessableEntityException("Password must contain 12 to 256 characters.");
            var email = await _challenges.ConsumeAsync("registration-complete", request.Token, cancellationToken)
                ?? throw new UnprocessableEntityException("Registration token is invalid or expired.");
            var hash = _passwords.Hash(request.Password);
            await _data.CreateVerifiedUserAsync(email, hash, cancellationToken);
            return new RegistrationResponseModel { State = "REGISTRATION_COMPLETED" };
        }
        throw new UnprocessableEntityException("Invalid registration stage.");
    }

    public async Task<RegistrationResponseModel> VerifyEmailAsync(VerifyEmailRequestModel request, CancellationToken cancellationToken = default)
    {
        var completion = await _challenges.ExchangeAsync("registration-verify", request.Token, "registration-complete", _options.RegistrationCompletionTtl, cancellationToken)
            ?? throw new UnprocessableEntityException("Verification token is invalid or expired.");
        return new RegistrationResponseModel { State = "EMAIL_VERIFIED", CompletionToken = completion };
    }

    public async Task<AuthenticationResultResponseModel> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default)
    {
        if (request.Operation == "AUTHENTICATE")
        {
            if (string.IsNullOrWhiteSpace(request.Identity) || request.Identity.Length > 255 || string.IsNullOrEmpty(request.Password) || request.Password.Length > 256) throw InvalidCredentials();
            var identity = request.Identity.Trim().ToLowerInvariant();
            if (!await _challenges.AllowAsync("login-ip", request.RemoteAddress, 60, TimeSpan.FromMinutes(15), cancellationToken) ||
                !await _challenges.AllowAsync("login-account", identity, 10, TimeSpan.FromMinutes(15), cancellationToken)) throw InvalidCredentials();
            var matches = await _data.FindCredentialsAsync(identity, cancellationToken);
            var passwordMatches = _passwords.Verify(request.Password, matches.Count == 1 ? matches[0].PasswordHash : DummyPasswordHash);
            if (matches.Count != 1 || !matches[0].User.IsEmailVerified || matches[0].User.Status != UserStatus.Active || !passwordMatches)
                throw InvalidCredentials();
            return await BeginAsync(matches[0].UserId, "password", cancellationToken);
        }
        if (request.Operation == "ROLE_SELECTION_INIT")
        {
            var userId = ParseChallenge(await _challenges.FindAsync("role-selection", request.RoleSelectionToken, cancellationToken));
            var roles = await EligibleRolesAsync(userId, cancellationToken);
            return new AuthenticationResultResponseModel { State = "ROLE_SELECTION_REQUIRED", Roles = roles };
        }
        if (request.Operation == "SELECT_ROLE")
        {
            var challenge = await _challenges.FindAsync("role-selection", request.RoleSelectionToken, cancellationToken);
            var userId = ParseChallenge(challenge);
            var roles = await EligibleRolesAsync(userId, cancellationToken);
            if (request.Role == null || !roles.Contains(request.Role, StringComparer.Ordinal)) throw InvalidCredentials();
            var consumed = await _challenges.ConsumeAsync("role-selection", request.RoleSelectionToken, cancellationToken);
            if (consumed != challenge || !(await EligibleRolesAsync(userId, cancellationToken)).Contains(request.Role, StringComparer.Ordinal)) throw InvalidCredentials();
            return await IssueAsync(userId, request.Role, cancellationToken);
        }
        throw new UnprocessableEntityException("Invalid login operation.");
    }

    public async Task<AuthenticationResultResponseModel> RefreshAsync(RefreshRequestModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            var principal = _tokens.ValidateRefreshToken(request.RefreshToken ?? string.Empty);
            var userId = Guid.Parse(principal.FindFirst("sub")!.Value);
            var role = principal.FindFirst("activeRole")!.Value;
            var session = principal.FindFirst("jti")!.Value;
            if (!(await EligibleRolesAsync(userId, cancellationToken)).Contains(role, StringComparer.Ordinal)) throw InvalidCredentials();
            return new AuthenticationResultResponseModel { State = "AUTHENTICATED", UserId = userId, ActiveRole = role,
                AccessToken = _tokens.CreateAccessToken(userId, role, session), ExpiresIn = (long)_options.AccessToken.Expiration.TotalSeconds };
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException or FormatException)
        {
            throw InvalidCredentials();
        }
    }

    public async Task<AuthenticationResultResponseModel> GoogleLoginAsync(GoogleLoginRequestModel request, CancellationToken cancellationToken = default)
    {
        var id = await _data.ResolveGoogleUserAsync(request.Subject, request.Email, request.EmailVerified, cancellationToken);
        return await BeginAsync(id, "google", cancellationToken);
    }

    private async Task<AuthenticationResultResponseModel> BeginAsync(Guid userId, string method, CancellationToken cancellationToken)
    {
        var roles = await EligibleRolesAsync(userId, cancellationToken);
        if (roles.Count == 1) return await IssueAsync(userId, roles[0], cancellationToken);
        var challenge = await _challenges.IssueAsync("role-selection", $"{userId}:{method}", _options.RoleSelectionTtl, cancellationToken);
        return new AuthenticationResultResponseModel { State = "ROLE_SELECTION_REQUIRED", Roles = roles, RoleSelectionToken = challenge };
    }

    private async Task<IReadOnlyList<string>> EligibleRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await _data.FindEligibleRolesAsync(userId, cancellationToken);
        return roles.Count == 0 ? throw InvalidCredentials() : roles;
    }

    private async Task<AuthenticationResultResponseModel> IssueAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        var user = await _data.FindUserAsync(userId, cancellationToken);
        if (user == null || user.IsDeleted || !user.IsEmailVerified || user.Status != UserStatus.Active) throw InvalidCredentials();
        var refresh = _tokens.CreateRefreshToken(userId, role);
        var session = _tokens.ValidateRefreshToken(refresh).FindFirst("jti")!.Value;
        return new AuthenticationResultResponseModel { State = "AUTHENTICATED", UserId = userId, UserName = user.UserName, Email = user.Email,
            ActiveRole = role, RefreshToken = refresh, CsrfToken = _tokens.CreateCsrfToken(session),
            AccessToken = _tokens.CreateAccessToken(userId, role, session), ExpiresIn = (long)_options.AccessToken.Expiration.TotalSeconds };
    }

    private static Guid ParseChallenge(string? challenge)
    {
        var parts = challenge?.Split(':');
        return parts is { Length: 2 } && (parts[1] == "password" || parts[1] == "google") && Guid.TryParse(parts[0], out var id)
            ? id : throw InvalidCredentials();
    }

    private static string NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 255 || normalized.Contains('\r') || normalized.Contains('\n') ||
            !MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized || !normalized.Contains('@'))
            throw new UnprocessableEntityException("Invalid email address.");
        return normalized;
    }

    private static UnauthorizedException InvalidCredentials() => new("Invalid authentication credentials");
}

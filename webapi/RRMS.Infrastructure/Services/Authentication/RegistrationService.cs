using System.Net.Mail;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Constants.Authentication;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Coordinates staged registration and email verification.
/// It issues and consumes verification challenges before creating a verified account.
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly IAuthenticationChallengeStore _challenges;
    private readonly IAuthenticationDataService _data;
    private readonly IAuthenticationEmailService _email;
    private readonly ICurrentRequestContext _currentRequestContext;
    private readonly IPasswordService _passwords;
    private readonly CredentialAuthOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly Serilog.ILogger _logger;

    public RegistrationService(IAuthenticationChallengeStore challenges, IAuthenticationDataService data,
        IAuthenticationEmailService email, IPasswordService passwords, IOptions<AuthOptions> options,
        IHostEnvironment environment, Serilog.ILogger logger, ICurrentRequestContext currentRequestContext)
    {
        _challenges = challenges;
        _data = data;
        _email = email;
        _currentRequestContext = currentRequestContext;
        _passwords = passwords;
        _options = options.Value.Credentials;
        _environment = environment;
        _logger = logger;
    }

    public async Task<RegistrationResponseModel> RegisterAsync(RegistrationRequestModel request, CancellationToken cancellationToken = default)
    {
        if (request.Stage == RegistrationStages.Initiate)
        {
            var email = NormalizeEmail(request.Email);
            if (!Uri.TryCreate(_options.VerificationUrl, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
                !(uri.Scheme == Uri.UriSchemeHttps || (_environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)))
            {
                throw new InvalidOperationException("Invalid verification URL configuration.");
            }

            if (await _challenges.AllowAsync("register-ip", _currentRequestContext.RemoteIpAddress ?? "unknown", 20, TimeSpan.FromHours(1), cancellationToken) &&
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
            return new RegistrationResponseModel
            {
                State = AuthenticationStates.RegistrationPending
            };
        }
        if (request.Stage == RegistrationStages.Complete)
        {
            if (request.Password is null || request.Password.Length < 12 || request.Password.Length > 256)
            {
                throw new UnprocessableEntityException("Password must contain 12 to 256 characters.");
            }

            var email = await _challenges.ConsumeAsync("registration-complete", request.Token, cancellationToken)
                ?? throw new UnprocessableEntityException("Registration token is invalid or expired.");
            var hash = _passwords.Hash(request.Password);
            await _data.CreateVerifiedUserAsync(email, hash, cancellationToken);
            return new RegistrationResponseModel
            {
                State = AuthenticationStates.RegistrationCompleted
            };
        }
        throw new UnprocessableEntityException("Invalid registration stage.");
    }

    public async Task<VerifyEmailResponseModel> VerifyEmailAsync(VerifyEmailRequestModel request, CancellationToken cancellationToken = default)
    {
        var completion = await _challenges.ExchangeAsync("registration-verify", request.Token, "registration-complete", _options.RegistrationCompletionTtl, cancellationToken)
            ?? throw new UnprocessableEntityException("Verification token is invalid or expired.");
        return new VerifyEmailResponseModel
        {
            State = AuthenticationStates.EmailVerified,
            CompletionToken = completion
        };
    }

    private static string NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 255 || normalized.Contains('\r') || normalized.Contains('\n') ||
            !MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized || !normalized.Contains('@'))
        {
            throw new UnprocessableEntityException("Invalid email address.");
        }

        return normalized;
    }

}

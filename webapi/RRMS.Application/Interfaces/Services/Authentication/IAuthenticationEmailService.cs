namespace RRMS.Application.Interfaces.Services.Authentication;

/// <summary>
/// Sends the verification link needed to complete account registration.
/// </summary>
public interface IAuthenticationEmailService
{
    Task SendVerificationAsync(string recipient, string verificationLink, CancellationToken cancellationToken = default);
}

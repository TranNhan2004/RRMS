namespace RRMS.Application.Interfaces.Services.Authentication;

public interface IAuthenticationEmailService
{
    void ValidateConfiguration();
    Task SendVerificationAsync(string recipient, string verificationLink, CancellationToken cancellationToken = default);
}

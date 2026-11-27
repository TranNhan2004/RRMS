using RRMS.Application.Models.Authentication;

namespace RRMS.Application.Interfaces.Services.Authentication;

/// <summary>
/// Handles registration stages and email verification before an account is created.
/// </summary>
public interface IRegistrationService
{
    Task<RegistrationResponseModel> RegisterAsync(RegistrationRequestModel request, CancellationToken cancellationToken = default);
    Task<VerifyEmailResponseModel> VerifyEmailAsync(VerifyEmailRequestModel request, CancellationToken cancellationToken = default);
}

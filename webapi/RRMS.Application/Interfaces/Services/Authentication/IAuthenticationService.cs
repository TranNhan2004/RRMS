using RRMS.Application.Models.Authentication;

namespace RRMS.Application.Interfaces.Services.Authentication;

public interface IAuthenticationService
{
    Task<RegistrationResponseModel> RegisterAsync(RegistrationRequestModel request, CancellationToken cancellationToken = default);
    Task<RegistrationResponseModel> VerifyEmailAsync(VerifyEmailRequestModel request, CancellationToken cancellationToken = default);
    Task<AuthenticationResultResponseModel> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default);
    Task<AuthenticationResultResponseModel> RefreshAsync(RefreshRequestModel request, CancellationToken cancellationToken = default);
    Task<AuthenticationResultResponseModel> GoogleLoginAsync(GoogleLoginRequestModel request, CancellationToken cancellationToken = default);
}

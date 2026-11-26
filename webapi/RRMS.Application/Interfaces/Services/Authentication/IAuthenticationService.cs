using RRMS.Application.Models.Authentication;

namespace RRMS.Application.Interfaces.Services.Authentication;

/// <summary>
/// Authenticates users with credentials or Google and handles pending role selection.
/// It returns the authentication result; session cookies are established separately.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResultResponseModel> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken = default);
    Task<AuthenticationResultResponseModel> GetPendingSessionAsync(CancellationToken cancellationToken = default);
    Task<AuthenticationResultResponseModel> SelectRoleAsync(SelectRoleRequestModel request, CancellationToken cancellationToken = default);
    Task<AuthenticationResultResponseModel> GoogleLoginAsync(GoogleLoginRequestModel request, CancellationToken cancellationToken = default);
}

using RRMS.Application.Models.Authentication;

namespace RRMS.Application.Interfaces.Services.Authentication;

/// <summary>
/// Manages the current user’s sessions, including listing, revocation, logout, and active-role switching.
/// </summary>
public interface ISessionManagementService
{
    Task<IReadOnlyList<SessionResponseModel>> GetSessionsAsync(CancellationToken cancellationToken);
    Task<bool> RevokeSessionAsync(string reference, CancellationToken cancellationToken);
    Task LogoutAsync(CancellationToken cancellationToken);
    Task LogoutAllAsync(CancellationToken cancellationToken);
    Task SwitchRoleAsync(string roleCode, CancellationToken cancellationToken);
}

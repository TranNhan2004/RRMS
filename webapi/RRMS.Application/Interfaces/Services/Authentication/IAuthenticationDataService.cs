using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Services.Authentication;

public interface IAuthenticationDataService
{
    Task<IReadOnlyList<UserCredential>> FindCredentialsAsync(string identity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> FindEligibleRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> FindEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task CreateVerifiedUserAsync(string email, string passwordHash, CancellationToken cancellationToken = default);
    Task<Guid> ResolveGoogleUserAsync(string subject, string? email, bool emailVerified, CancellationToken cancellationToken = default);
}

using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IUserIdentityRepository : IGenericRepository<UserIdentity>
{
    Task<Guid?> FindGoogleUserIdAsync(string subject, CancellationToken cancellationToken = default);
    Task<bool> FindGoogleLinkExistsAsync(Guid userId, string subject, CancellationToken cancellationToken = default);
}

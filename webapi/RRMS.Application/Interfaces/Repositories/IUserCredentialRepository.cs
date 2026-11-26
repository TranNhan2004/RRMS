using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IUserCredentialRepository : IGenericRepository<UserCredential>
{
    Task<IReadOnlyList<UserCredential>> FindByIdentityAsync(string identity, CancellationToken cancellationToken = default);
}

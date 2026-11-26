using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IRoleRepository : IGenericRepository<Role>
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
}

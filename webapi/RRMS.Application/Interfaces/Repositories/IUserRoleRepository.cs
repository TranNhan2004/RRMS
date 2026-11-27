using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IUserRoleRepository : IGenericRepository<UserRole>
{
    Task<IReadOnlyList<string>> FindEligibleRoleCodesAsync(Guid userId, CancellationToken cancellationToken = default);
}

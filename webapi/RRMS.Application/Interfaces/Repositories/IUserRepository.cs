using RRMS.Application.Models.Users;
using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<GetCurrentUserResponseModel?> FindCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

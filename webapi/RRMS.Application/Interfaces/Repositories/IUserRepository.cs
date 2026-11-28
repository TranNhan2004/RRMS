using RRMS.Application.Models.Users;
using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Guid? FindUserIdByEmail(string email);
    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> FindEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<GetCurrentUserResponseModel?> FindCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

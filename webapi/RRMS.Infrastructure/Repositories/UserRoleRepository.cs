using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Users;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }

    public async Task<IReadOnlyList<string>> FindEligibleRoleCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var query = from userRole in _dbContext.Set<UserRole>().AsNoTracking()
                    join user in _dbContext.Set<User>().AsNoTracking() on userRole.UserId equals user.Id
                    join role in _dbContext.Set<Role>().AsNoTracking() on userRole.RoleId equals role.Id
                    where user.Id == userId && user.IsEmailVerified && user.Status == UserStatus.Active
                    orderby role.Code
                    select role.Code;
        return await query.Distinct().ToListAsync(cancellationToken);
    }
}

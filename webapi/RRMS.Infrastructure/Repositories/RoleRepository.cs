using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }

    public Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Set<Role>().AsNoTracking().SingleOrDefaultAsync(role => role.Code == code, cancellationToken);
}

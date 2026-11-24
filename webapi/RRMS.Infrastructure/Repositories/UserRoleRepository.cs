using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }

    
}

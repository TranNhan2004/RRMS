using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class UserProfileRepository : GenericRepository<UserProfile>, IUserProfileRepository
{
    public UserProfileRepository(Serilog.ILogger logger, AppDbContext context) : base(logger, context)
    {
    }
}

using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Models.Users;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(Serilog.ILogger logger, AppDbContext context) : base(logger, context)
    {
    }

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Set<User>().AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<bool> FindEmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Set<User>().IgnoreQueryFilters().AnyAsync(user => user.Email.ToLower() == email, cancellationToken);

    public Task<GetCurrentUserResponseModel?> FindCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var query = from user in _dbContext.Set<User>().AsNoTracking()
                    join profile in _dbContext.Set<UserProfile>().AsNoTracking().Where(p => !p.IsDeleted)
                        on user.Id equals profile.UserId into profiles
                    from profile in profiles.DefaultIfEmpty()
                    where user.Id == userId && !user.IsDeleted
                    select new GetCurrentUserResponseModel
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        Profile = profile == null ? null : new GetCurrentUserProfileResponseModel
                        {
                            NationalIdNumber = profile.NationalIdNumber,
                            FirstName = profile.FirstName,
                            LastName = profile.LastName,
                            BirthDate = profile.BirthDate,
                            Sex = profile.Sex,
                            AddressUpdatedAt = profile.AddressUpdatedAt
                        }
                    };
        return query.SingleOrDefaultAsync(cancellationToken);
    }
}

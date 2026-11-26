using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class UserIdentityRepository : GenericRepository<UserIdentity>, IUserIdentityRepository
{
    public UserIdentityRepository(Serilog.ILogger logger, AppDbContext context) : base(logger, context)
    {
    }

    public Task<Guid?> FindGoogleUserIdAsync(string subject, CancellationToken cancellationToken = default) =>
        _dbContext.Set<UserIdentity>().AsNoTracking()
            .Where(identity => identity.Provider == UserIdentityProvider.Google && identity.ProviderSubject == subject)
            .Select(identity => (Guid?)identity.UserId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> FindGoogleLinkExistsAsync(Guid userId, string subject, CancellationToken cancellationToken = default) =>
        _dbContext.Set<UserIdentity>().AnyAsync(identity => identity.UserId == userId ||
            (identity.Provider == UserIdentityProvider.Google && identity.ProviderSubject == subject), cancellationToken);
}

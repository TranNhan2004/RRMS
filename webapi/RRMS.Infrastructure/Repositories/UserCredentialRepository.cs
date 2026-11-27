using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class UserCredentialRepository : GenericRepository<UserCredential>, IUserCredentialRepository
{
    public UserCredentialRepository(Serilog.ILogger logger, AppDbContext context) : base(logger, context)
    {
    }

    public async Task<IReadOnlyList<UserCredential>> FindByIdentityAsync(string identity, CancellationToken cancellationToken = default)
    {
        var query = from credential in _dbContext.Set<UserCredential>().AsNoTracking().Include(value => value.User)
                    where credential.User.Email.ToLower() == identity ||
                        (credential.User.UserName != null && credential.User.UserName.ToLower() == identity)
                    select credential;
        return await query.Take(2).ToListAsync(cancellationToken);
    }
}

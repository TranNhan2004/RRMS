using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;
using RRMS.Domain.Enums.Users;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Services.Authentication;

public class AuthenticationDataService : IAuthenticationDataService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public AuthenticationDataService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserCredential>> FindCredentialsAsync(string identity, CancellationToken cancellationToken = default)
    {
        var query = from credential in _context.UserCredentials.AsNoTracking().Include(x => x.User)
                    where credential.User.Email.ToLower() == identity || (credential.User.UserName != null && credential.User.UserName.ToLower() == identity)
                    select credential;
        return await query.Take(2).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> FindEligibleRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var query = from userRole in _context.UserRoles.AsNoTracking()
                    join user in _context.Users.AsNoTracking() on userRole.UserId equals user.Id
                    join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    where user.Id == userId && user.IsEmailVerified && user.Status == UserStatus.Active
                    orderby role.Code
                    select role.Code;
        return await query.Distinct().ToListAsync(cancellationToken);
    }

    public Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<bool> FindEmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.IgnoreQueryFilters().AnyAsync(user => user.Email.ToLower() == email, cancellationToken);

    public async Task CreateVerifiedUserAsync(string email, string passwordHash, CancellationToken cancellationToken = default)
    {
        if (await FindEmailExistsAsync(email, cancellationToken)) throw new InvalidOperationException("Registration cannot be completed.");
        var role = await _context.Roles.SingleOrDefaultAsync(x => x.Code == "TENANT", cancellationToken)
            ?? throw new InvalidOperationException("Default registration role is unavailable.");
        var user = NewUser(email);
        _context.Users.Add(user);
        _context.UserCredentials.Add(new UserCredential { Id = Guid.CreateVersion7(), UserId = user.Id, PasswordHash = passwordHash });
        _context.UserProfiles.Add(new UserProfile { Id = Guid.CreateVersion7(), UserId = user.Id });
        _context.UserRoles.Add(new UserRole { Id = Guid.CreateVersion7(), UserId = user.Id, RoleId = role.Id });
        await _unitOfWork.SaveChangesAsync(user.Id.ToString(), cancellationToken);
    }

    public async Task<Guid> ResolveGoogleUserAsync(string subject, string? email, bool emailVerified, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255) throw new InvalidOperationException("Invalid Google identity.");
        var existing = await _context.UserIdentities.AsNoTracking().Where(x => x.Provider == UserIdentityProvider.Google && x.ProviderSubject == subject)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken);
        if (existing.HasValue) return existing.Value;
        if (!emailVerified || string.IsNullOrWhiteSpace(email) || email.Length > 255) throw new InvalidOperationException("A verified Google email is required.");
        var normalized = email.Trim().ToLowerInvariant();
        if (await FindEmailExistsAsync(normalized, cancellationToken)) throw new InvalidOperationException("Account linking is required.");
        var role = await _context.Roles.SingleOrDefaultAsync(x => x.Code == "TENANT", cancellationToken)
            ?? throw new InvalidOperationException("Default registration role is unavailable.");
        var user = NewUser(normalized);
        _context.Users.Add(user);
        _context.UserProfiles.Add(new UserProfile { Id = Guid.CreateVersion7(), UserId = user.Id });
        _context.UserRoles.Add(new UserRole { Id = Guid.CreateVersion7(), UserId = user.Id, RoleId = role.Id });
        _context.UserIdentities.Add(new UserIdentity { Id = Guid.CreateVersion7(), UserId = user.Id, Provider = UserIdentityProvider.Google, ProviderSubject = subject });
        await _unitOfWork.SaveChangesAsync(user.Id.ToString(), cancellationToken);
        return user.Id;
    }

    private static User NewUser(string email) => new() { Id = Guid.CreateVersion7(), Email = email, Status = UserStatus.Active, IsEmailVerified = true };
}

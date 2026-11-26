using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Domain.Entities;
using RRMS.Domain.Constants.Uam;
using RRMS.Domain.Enums.Uam;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Coordinates authentication-related account lookups and account creation through repositories.
/// It commits new verified users and Google identities through the unit of work.
/// </summary>
public class AuthenticationDataService : IAuthenticationDataService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserCredentialRepository _userCredentialRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuthenticationDataService(
        IUserRepository userRepository,
        IUserCredentialRepository userCredentialRepository,
        IUserProfileRepository userProfileRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userCredentialRepository = userCredentialRepository;
        _userProfileRepository = userProfileRepository;
        _userIdentityRepository = userIdentityRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<UserCredential>> FindCredentialsAsync(string identity, CancellationToken cancellationToken = default) =>
        _userCredentialRepository.FindByIdentityAsync(identity, cancellationToken);

    public Task<IReadOnlyList<string>> FindEligibleRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _userRoleRepository.FindEligibleRoleCodesAsync(userId, cancellationToken);

    public Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _userRepository.FindByIdAsync(userId, cancellationToken);

    public Task<bool> FindEmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _userRepository.FindEmailExistsAsync(email, cancellationToken);

    public async Task CreateVerifiedUserAsync(string email, string passwordHash, CancellationToken cancellationToken = default)
    {
        if (await FindEmailExistsAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("Registration cannot be completed.");
        }

        var role = await _roleRepository.FindByCodeAsync(RoleCodes.Tenant, cancellationToken)
            ?? throw new InvalidOperationException("Default registration role is unavailable.");
        var user = NewUser(email);
        _userRepository.Add(user);
        _userCredentialRepository.Add(new UserCredential
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            PasswordHash = passwordHash
        });
        _userProfileRepository.Add(new UserProfile
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id
        });
        _userRoleRepository.Add(new UserRole
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            RoleId = role.Id
        });
        await _unitOfWork.SaveChangesAsync(user.Id.ToString(), cancellationToken);
    }

    public async Task<Guid> ResolveGoogleUserAsync(string subject, string? email, bool emailVerified, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
        {
            throw new UnauthorizedException("Invalid Google identity.");
        }

        var existing = await _userIdentityRepository.FindGoogleUserIdAsync(subject, cancellationToken);

        if (existing.HasValue)
        {
            return existing.Value;
        }

        if (!emailVerified || string.IsNullOrWhiteSpace(email) || email.Length > 255)
        {
            throw new UnauthorizedException("A verified Google email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (await FindEmailExistsAsync(normalized, cancellationToken))
        {
            throw new ConflictException("Sign in to your existing account to link Google.");
        }

        var role = await _roleRepository.FindByCodeAsync(RoleCodes.Tenant, cancellationToken)
            ?? throw new InvalidOperationException("Default registration role is unavailable.");
        var user = NewUser(normalized);
        _userRepository.Add(user);
        _userProfileRepository.Add(new UserProfile
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id
        });
        _userRoleRepository.Add(new UserRole
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            RoleId = role.Id
        });
        _userIdentityRepository.Add(new UserIdentity
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Provider = UserIdentityProvider.Google,
            ProviderSubject = subject
        });
        await _unitOfWork.SaveChangesAsync(user.Id.ToString(), cancellationToken);
        return user.Id;
    }

    private static User NewUser(string email) => new()
    {
        Id = Guid.CreateVersion7(),
        Email = email,
        Status = UserStatus.Active,
        IsEmailVerified = true
    };
}

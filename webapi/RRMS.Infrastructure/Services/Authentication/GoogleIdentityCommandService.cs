using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Exceptions;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Links a Google identity to an existing user after checking the verified email and existing links.
/// </summary>
public class GoogleIdentityCommandService : IGoogleIdentityCommandService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GoogleIdentityCommandService(IUserRepository userRepository, IUserIdentityRepository userIdentityRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task LinkAsync(
        Guid userId,
        string subject,
        string? email,
        bool emailVerified,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255 || !emailVerified || string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedException("A verified Google identity is required.");
        }

        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsEmailVerified || user.Status != UserStatus.Active ||
            !string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("Google identity cannot be linked to this account.");
        }

        if (await _userIdentityRepository.FindGoogleLinkExistsAsync(userId, subject, cancellationToken))
        {
            throw new ConflictException("Google identity is already linked.");
        }

        _userIdentityRepository.Add(new UserIdentity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Provider = UserIdentityProvider.Google,
            ProviderSubject = subject
        });
        await _unitOfWork.SaveChangesAsync(userId.ToString(), cancellationToken);
    }
}

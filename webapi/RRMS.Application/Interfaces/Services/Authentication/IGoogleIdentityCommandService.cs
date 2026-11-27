namespace RRMS.Application.Interfaces.Services.Authentication;

/// <summary>
/// Links a verified Google identity to an existing authenticated user account.
/// </summary>
public interface IGoogleIdentityCommandService
{
    Task LinkAsync(Guid userId, string subject, string? email, bool emailVerified, CancellationToken cancellationToken = default);
}

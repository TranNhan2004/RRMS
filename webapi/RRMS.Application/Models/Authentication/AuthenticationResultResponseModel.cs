namespace RRMS.Application.Models.Authentication;

public class AuthenticationResultResponseModel
{
    public string State { get; init; } = string.Empty;
    public string? AuthenticationMethod { get; init; }
    public DateTimeOffset? PendingExpiresAt { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? ActiveRole { get; init; }
    public IReadOnlyList<string>? Roles { get; init; }
}

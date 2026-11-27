using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class LoginResponse
{
    public string State { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? ActiveRole { get; init; }
    public DateTimeOffset? PendingExpiresAt { get; init; }
    public IReadOnlyList<string>? EligibleRoles { get; init; }
    public static LoginResponse FromModel(AuthenticationResultResponseModel model) => new()
    {
        State = model.State,
        UserId = model.UserId,
        UserName = model.UserName,
        ActiveRole = model.ActiveRole,
        EligibleRoles = model.Roles,
        PendingExpiresAt = model.PendingExpiresAt
    };
}

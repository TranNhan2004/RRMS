using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class RegistrationResponse
{
    public string State { get; init; } = string.Empty;
    public string? CompletionToken { get; init; }
    public static RegistrationResponse FromModel(RegistrationResponseModel model) => new() { State = model.State, CompletionToken = model.CompletionToken };
}

public class VerifyEmailResponse
{
    public string State { get; init; } = string.Empty;
    public string? CompletionToken { get; init; }
    public static VerifyEmailResponse FromModel(RegistrationResponseModel model) => new() { State = model.State, CompletionToken = model.CompletionToken };
}

public class LoginResponse
{
    public string State { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public string? TokenType { get; init; }
    public long? ExpiresIn { get; init; }
    public Guid? UserId { get; init; }
    public LoginUserResponse? User { get; init; }
    public string? ActiveRole { get; init; }
    public IReadOnlyList<string>? Roles { get; init; }
    public string? RoleSelectionToken { get; init; }

    public static LoginResponse FromModel(AuthenticationResultResponseModel model) => new()
    {
        State = model.State, AccessToken = model.AccessToken, TokenType = model.AccessToken == null ? null : "Bearer",
        ExpiresIn = model.ExpiresIn, UserId = model.UserId, User = model.UserId.HasValue ? LoginUserResponse.FromModel(model) : null,
        ActiveRole = model.ActiveRole, Roles = model.Roles, RoleSelectionToken = model.RoleSelectionToken
    };
}

public class LoginUserResponse
{
    public Guid? Id { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public static LoginUserResponse FromModel(AuthenticationResultResponseModel model) => new() { Id = model.UserId, UserName = model.UserName, Email = model.Email };
}

public class RefreshResponse
{
    public string? AccessToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public long? ExpiresIn { get; init; }
    public Guid? UserId { get; init; }
    public string? ActiveRole { get; init; }
    public static RefreshResponse FromModel(AuthenticationResultResponseModel model) => new()
    {
        AccessToken = model.AccessToken, ExpiresIn = model.ExpiresIn, UserId = model.UserId, ActiveRole = model.ActiveRole
    };
}

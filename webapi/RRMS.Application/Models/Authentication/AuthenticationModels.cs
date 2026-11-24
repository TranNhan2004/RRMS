namespace RRMS.Application.Models.Authentication;

public class LoginRequestModel
{
    public string Operation { get; init; } = string.Empty;
    public string? Identity { get; init; }
    public string? Password { get; init; }
    public string? RoleSelectionToken { get; init; }
    public string? Role { get; init; }
    public string RemoteAddress { get; init; } = string.Empty;
}

public class RegistrationRequestModel
{
    public string Stage { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Token { get; init; }
    public string? Password { get; init; }
    public string RemoteAddress { get; init; } = string.Empty;
}

public class RegistrationResponseModel
{
    public string State { get; init; } = string.Empty;
    public string? CompletionToken { get; init; }
}

public class AuthenticationResultResponseModel
{
    public string State { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? CsrfToken { get; init; }
    public string? RoleSelectionToken { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? ActiveRole { get; init; }
    public IReadOnlyList<string>? Roles { get; init; }
    public long? ExpiresIn { get; init; }
}

public class VerifyEmailRequestModel
{
    public string Token { get; init; } = string.Empty;
}

public class RefreshRequestModel
{
    public string? RefreshToken { get; init; }
}

public class GoogleLoginRequestModel
{
    public string Subject { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool EmailVerified { get; init; }
}

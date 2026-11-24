using System.ComponentModel.DataAnnotations;
using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class RegistrationRequest : IValidatableObject
{
    [Required] public string Stage { get; init; } = string.Empty;
    [EmailAddress, MaxLength(255)] public string? Email { get; init; }
    [MaxLength(128)] public string? Token { get; init; }
    [MaxLength(256)] public string? Password { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var valid = Stage == "INITIATE" ? !string.IsNullOrWhiteSpace(Email) && Token == null && Password == null :
            Stage == "COMPLETE" && Email == null && !string.IsNullOrWhiteSpace(Token) && Password is { Length: >= 12 };
        if (!valid) yield return new ValidationResult("Invalid registration stage or fields.");
    }

    public static RegistrationRequestModel ToModel(RegistrationRequest request, string remoteAddress) => new()
    {
        Stage = request.Stage, Email = request.Email, Token = request.Token, Password = request.Password, RemoteAddress = remoteAddress
    };
}

public class VerifyEmailRequest
{
    [Required, MaxLength(128)] public string Token { get; init; } = string.Empty;
    public static VerifyEmailRequestModel ToModel(VerifyEmailRequest request) => new() { Token = request.Token };
}

public class RefreshRequest
{
    public static RefreshRequestModel ToModel(string? refreshToken) => new() { RefreshToken = refreshToken };
}

public class LoginRequest : IValidatableObject
{
    [Required] public string Operation { get; init; } = string.Empty;
    [MaxLength(255)] public string? Identity { get; init; }
    [MaxLength(256)] public string? Password { get; init; }
    [MaxLength(128)] public string? RoleSelectionToken { get; init; }
    [MaxLength(64)] public string? Role { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var valid = Operation switch
        {
            "AUTHENTICATE" => !string.IsNullOrWhiteSpace(Identity) && !string.IsNullOrEmpty(Password) && RoleSelectionToken == null && Role == null,
            "ROLE_SELECTION_INIT" => Identity == null && Password == null && RoleSelectionToken == null && Role == null,
            "SELECT_ROLE" => Identity == null && Password == null && !string.IsNullOrWhiteSpace(Role),
            _ => false
        };
        if (!valid) yield return new ValidationResult("Invalid login operation or fields.");
    }

    public static LoginRequestModel ToModel(LoginRequest request, string remoteAddress, string? cookieToken) => new()
    {
        Operation = request.Operation, Identity = request.Identity, Password = request.Password,
        RoleSelectionToken = request.RoleSelectionToken ?? cookieToken, Role = request.Role, RemoteAddress = remoteAddress
    };
}

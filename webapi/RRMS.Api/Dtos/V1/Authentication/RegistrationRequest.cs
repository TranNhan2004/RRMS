using RRMS.Domain.Constants.Authentication;
using System.ComponentModel.DataAnnotations;
using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class RegistrationRequest : IValidatableObject
{
    [Required]
    public string Stage { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }

    [MaxLength(128)]
    public string? Token { get; init; }

    [MaxLength(256)]
    public string? Password { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var valid = Stage == RegistrationStages.Initiate ? !string.IsNullOrWhiteSpace(Email) && Token == null && Password == null :
            Stage == RegistrationStages.Complete && Email == null && !string.IsNullOrWhiteSpace(Token) && Password is { Length: >= 12 };
        if (!valid)
        {
            yield return new ValidationResult("Invalid registration stage or fields.");
        }
    }

    public static RegistrationRequestModel ToModel(RegistrationRequest request) => new()
    {
        Stage = request.Stage,
        Email = request.Email,
        Token = request.Token,
        Password = request.Password
    };
}
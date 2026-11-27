namespace RRMS.Application.Models.Authentication;

public class RegistrationRequestModel
{
    public string Stage { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Token { get; init; }
    public string? Password { get; init; }
}

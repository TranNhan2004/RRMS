namespace RRMS.Application.Models.Authentication;

public class RegistrationResponseModel
{
    public string State { get; init; } = string.Empty;
    public string? CompletionToken { get; init; }
}

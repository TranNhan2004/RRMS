using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class RegistrationResponse
{
    public string State { get; init; } = string.Empty;
    public string? CompletionToken { get; init; }
    public static RegistrationResponse FromModel(RegistrationResponseModel model) => new()
    {
        State = model.State,
        CompletionToken = model.CompletionToken
    };
}

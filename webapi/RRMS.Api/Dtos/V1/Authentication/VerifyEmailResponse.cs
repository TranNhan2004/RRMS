using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class VerifyEmailResponse
{
    public string State { get; init; } = string.Empty;
    public string? CompletionToken { get; init; }
    public static VerifyEmailResponse FromModel(VerifyEmailResponseModel model) => new()
    {
        State = model.State,
        CompletionToken = model.CompletionToken
    };
}

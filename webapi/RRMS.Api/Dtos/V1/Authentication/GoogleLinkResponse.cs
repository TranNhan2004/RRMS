using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class GoogleLinkResponse
{
    public string AuthorizationUrl { get; init; } = string.Empty;

    public static GoogleLinkResponse FromModel(GoogleLinkResponseModel model) => new()
    {
        AuthorizationUrl = model.AuthorizationUrl
    };
}

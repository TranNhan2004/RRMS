using RRMS.Application.Models.FileStorages;

namespace RRMS.Api.Dtos.V1.FileStorages;

public class GetFileUrlResponse
{
    public string Url { get; set; } = null!;

    public static GetFileUrlResponse FromModel(GetFileUrlResponseModel model) => new()
    {
        Url = model.Url
    };
}

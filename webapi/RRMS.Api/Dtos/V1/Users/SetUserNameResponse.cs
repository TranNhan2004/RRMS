using RRMS.Application.Models.Users;

namespace RRMS.Api.Dtos.V1.Users;

public class SetUserNameResponse
{
    public string UserName { get; init; } = null!;

    public static SetUserNameResponse FromModel(SetUserNameResponseModel model) => new()
    {
        UserName = model.UserName
    };
}

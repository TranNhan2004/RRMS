using System.ComponentModel.DataAnnotations;
using RRMS.Application.Models.Users;

namespace RRMS.Api.Dtos.V1.Users;

public class SetUserNameRequest
{
    [Required]
    [StringLength(64)]
    public string UserName { get; init; } = null!;

    public static SetUserNameRequestModel ToModel(SetUserNameRequest request) => new()
    {
        UserName = request.UserName
    };
}

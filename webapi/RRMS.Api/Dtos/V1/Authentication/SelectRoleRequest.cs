using RRMS.Domain.Constants.Authentication;
using System.ComponentModel.DataAnnotations;
using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class SelectRoleRequest
{
    [Required]
    [MaxLength(64)]
    public string RoleCode { get; init; } = string.Empty;

    public static SelectRoleRequestModel ToModel(SelectRoleRequest request) => new()
    {
        RoleCode = request.RoleCode
    };
}

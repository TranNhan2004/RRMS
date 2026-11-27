using RRMS.Domain.Constants.Authentication;
using System.ComponentModel.DataAnnotations;
using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class LoginRequest
{
    [Required]
    [MaxLength(255)]
    public string Identity { get; init; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Password { get; init; } = string.Empty;

    public static LoginRequestModel ToModel(LoginRequest request) => new()
    {
        Identity = request.Identity,
        Password = request.Password
    };
}
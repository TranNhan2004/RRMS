using System.ComponentModel.DataAnnotations;
using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class VerifyEmailRequest
{
    [Required]
    [MaxLength(128)]
    public string Token { get; init; } = string.Empty;

    public static VerifyEmailRequestModel ToModel(VerifyEmailRequest request) => new()
    {
        Token = request.Token
    };
}
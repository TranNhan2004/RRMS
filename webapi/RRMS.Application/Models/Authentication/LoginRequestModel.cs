namespace RRMS.Application.Models.Authentication;

public class LoginRequestModel
{
    public string? Identity { get; init; }
    public string? Password { get; init; }
}

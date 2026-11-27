namespace RRMS.Application.Models.Authentication;

public class GoogleLoginRequestModel
{
    public string Subject { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool EmailVerified { get; init; }
}

namespace RRMS.Application.Models.Authentication;

public class SessionResponseModel
{
    public string Reference { get; init; } = string.Empty;
    public string? ActiveRole { get; init; }
    public DateTimeOffset? IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

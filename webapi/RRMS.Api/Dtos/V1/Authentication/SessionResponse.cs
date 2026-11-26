using RRMS.Application.Models.Authentication;

namespace RRMS.Api.Dtos.V1.Authentication;

public class SessionResponse
{
    public string Reference { get; init; } = string.Empty;
    public string? ActiveRole { get; init; }
    public DateTimeOffset? IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    public static SessionResponse FromModel(SessionResponseModel model) => new()
    {
        Reference = model.Reference,
        ActiveRole = model.ActiveRole,
        IssuedAt = model.IssuedAt,
        ExpiresAt = model.ExpiresAt
    };
}

namespace RRMS.Application.Interfaces.Contexts;

public interface ICurrentUserContext
{
    Guid UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    string? ActiveRole { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}

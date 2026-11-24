using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Domain.Exceptions;

namespace RRMS.Infrastructure.Contexts;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal Principal => IsAuthenticated ? _accessor.HttpContext!.User : throw new UnauthorizedException("User is not authenticated.");
    public Guid UserId => Guid.TryParse(Principal.FindFirstValue("sub"), out var userId) ? userId : throw new UnauthorizedException("Invalid authenticated user identifier.");
    public string? Email => Principal.FindFirstValue("email");
    public string? UserName => Principal.FindFirstValue("userName");
    public string? ActiveRole => Principal.FindFirstValue("activeRole");
    public IReadOnlyList<string> Roles => Principal.FindAll("roles").Select(claim => claim.Value).ToArray();
    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}

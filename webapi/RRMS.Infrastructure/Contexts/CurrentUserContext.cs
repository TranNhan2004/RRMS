using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Domain.Exceptions;
using RRMS.Domain.Constants.Authentication;

namespace RRMS.Infrastructure.Contexts;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => IsAuthenticated ? _accessor.HttpContext?.User : null;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(AuthenticationClaimTypes.Subject), out var userId) && userId != Guid.Empty ? userId : null;
    public string? Email => Principal?.FindFirstValue("email");
    public string? UserName => Principal?.FindFirstValue(AuthenticationClaimTypes.UserName);
    public string? ActiveRole => Principal?.FindFirstValue(AuthenticationClaimTypes.ActiveRole);
    public string? SessionReference => Principal?.FindFirstValue(AuthenticationClaimTypes.SessionReference);
    public IReadOnlyList<string> Roles => Principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? [];
    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid GetRequiredUserId() => UserId ?? throw new UnauthorizedException("Authenticated user identifier is unavailable.");
}

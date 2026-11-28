using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Domain.Exceptions;
using RRMS.Domain.Constants.Authentication;

namespace RRMS.Infrastructure.Contexts;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;

    public CurrentUserContext(
        IHttpContextAccessor accessor,
        IHostEnvironment environment,
        IConfiguration configuration,
        IUserRepository userRepository)
    {
        _accessor = accessor;
        _environment = environment;
        _configuration = configuration;
        _userRepository = userRepository;
    }

    private ClaimsPrincipal? Principal => IsAuthenticated ? _accessor.HttpContext?.User : null;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(AuthenticationClaimTypes.Subject), out var userId) && userId != Guid.Empty ? userId : null;
    public string? Email => Principal?.FindFirstValue("email");
    public string? UserName => Principal?.FindFirstValue(AuthenticationClaimTypes.UserName);
    public string? ActiveRole => Principal?.FindFirstValue(AuthenticationClaimTypes.ActiveRole);
    public string? SessionReference => Principal?.FindFirstValue(AuthenticationClaimTypes.SessionReference);
    public IReadOnlyList<string> Roles => Principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? [];
    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid GetRequiredUserId()
    {
        var bypassEmail = _configuration["LocalBypassUserEmail"];
        if (_environment.IsDevelopment() && !string.IsNullOrWhiteSpace(bypassEmail))
        {
            var normalizedEmail = bypassEmail.Trim().ToLowerInvariant();
            var user = _userRepository.FindFirstByConditionAsync(
                q => q.Where(e => e.Email.ToLower() == normalizedEmail)
            ).GetAwaiter().GetResult();
            return user is not null && user.Id != Guid.Empty
                ? user.Id
                : throw new UnauthorizedException("Local bypass user identifier is unavailable.");
        }

        return UserId ?? throw new UnauthorizedException("Authenticated user identifier is unavailable.");
    }
}

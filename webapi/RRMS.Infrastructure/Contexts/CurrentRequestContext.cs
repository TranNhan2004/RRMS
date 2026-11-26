using Microsoft.AspNetCore.Http;
using RRMS.Application.Interfaces.Contexts;

namespace RRMS.Infrastructure.Contexts;

public class CurrentRequestContext : ICurrentRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentRequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? RemoteIpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
    public string? TraceId => _accessor.HttpContext?.TraceIdentifier;
    public string? ClientActionId => _accessor.HttpContext?.Request.Headers["X-Client-Action-Id"].FirstOrDefault();
    public string? ClientRequestId => _accessor.HttpContext?.Request.Headers["X-Client-Request-Id"].FirstOrDefault();
}

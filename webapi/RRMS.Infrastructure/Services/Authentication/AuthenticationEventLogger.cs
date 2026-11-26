using RRMS.Application.Interfaces.Contexts;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Records structured authentication events with user, session, and request correlation information.
/// </summary>
public sealed class AuthenticationEventLogger
{
    private readonly ICurrentRequestContext _requestContext;
    private readonly Serilog.ILogger _logger;

    public AuthenticationEventLogger(ICurrentRequestContext requestContext, Serilog.ILogger logger)
    {
        _requestContext = requestContext;
        _logger = logger;
    }

    public void Log(string name, Guid? userId, string? activeRole, string? sessionReference = null)
    {
        _logger.Information("Authentication event {Event} for user {UserId} with role {ActiveRole}, session {SessionReference}, remote IP {RemoteIpAddress}, trace {TraceId}, client action {ClientActionId}, client request {ClientRequestId}, timestamp {Timestamp}",
            name, userId, activeRole, sessionReference, _requestContext.RemoteIpAddress, _requestContext.TraceId,
            _requestContext.ClientActionId, _requestContext.ClientRequestId, DateTimeOffset.UtcNow);
    }
}

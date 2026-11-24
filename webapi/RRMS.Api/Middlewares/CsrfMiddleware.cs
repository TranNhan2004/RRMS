using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Infrastructure.Options;

namespace RRMS.Api.Middlewares;

public class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IOptions<CsrfOptions> options)
    {
        if (IsSafeMethod(context.Request.Method) || IsAnonymousAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        string? sessionId = null;
        try
        {
            var refreshToken = context.Request.Cookies["RRMS_REFRESH"];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                sessionId = tokenService.ValidateRefreshToken(refreshToken).FindFirst("jti")?.Value;
            }
            else
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    sessionId = tokenService.ValidateAccessToken(authorization[7..].Trim()).FindFirst("sessionId")?.Value;
                }
            }
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            sessionId = null;
        }

        var csrf = options.Value;
        var cookie = context.Request.Cookies[csrf.CookieName];
        var header = context.Request.Headers[csrf.HeaderName].ToString();
        if (!tokenService.ValidateCsrfToken(sessionId, cookie, header))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

    private static bool IsAnonymousAuthentication(PathString path)
    {
        var segments = (path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 4 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) &&
            segments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
            segments[2].Equals("auth", StringComparison.OrdinalIgnoreCase) &&
            (segments[3].Equals("register", StringComparison.OrdinalIgnoreCase) ||
             segments[3].Equals("verify-email", StringComparison.OrdinalIgnoreCase) ||
             segments[3].Equals("login", StringComparison.OrdinalIgnoreCase));
    }
}

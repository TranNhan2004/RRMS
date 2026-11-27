using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using RRMS.Domain.Constants.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Creates and rotates authentication sessions and manages the pending-session cookie.
/// It applies the configured cookie settings and delegates ticket storage to RedisTicketStore.
/// </summary>
public class SessionCookieService
{
    private const string SessionIdClaim = "Microsoft.AspNetCore.Authentication.Cookies-SessionId";
    private readonly RedisTicketStore _store;
    private readonly IOptionsMonitor<CookieAuthenticationOptions> _options;
    private readonly SessionAuthOptions _session;
    private readonly AuthOptions _auth;
    private readonly IHostEnvironment _environment;

    public SessionCookieService(RedisTicketStore store, IOptionsMonitor<CookieAuthenticationOptions> options, IOptions<AuthOptions> auth, IHostEnvironment environment)
    {
        _environment = environment;
        _store = store;
        _options = options;
        _auth = auth.Value;
        _session = _auth.Session;
    }

    public string? ReadPending(HttpContext context) => context.Request.Cookies[_auth.PendingSession.Cookie.Name];

    public void WritePending(HttpContext context, string token)
    {
        context.Response.Cookies.Append(_auth.PendingSession.Cookie.Name, token, PendingOptions(context, _auth.Credentials.RoleSelectionTtl));
    }

    public void ClearPending(HttpContext context)
    {
        context.Response.Cookies.Delete(_auth.PendingSession.Cookie.Name, PendingOptions(context, TimeSpan.Zero));
    }

    private CookieOptions PendingOptions(HttpContext context, TimeSpan lifetime) => new()
    {
        HttpOnly = true,
        Secure = CookiePolicy.IsSecure(_environment, context),
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = lifetime
    };

    public async Task SignInAsync(HttpContext context, AuthenticationResultResponseModel result)
    {
        var now = DateTimeOffset.UtcNow;
        var principal = Principal(
            result.UserId!.Value,
            result.UserName,
            result.Email,
            result.ActiveRole!,
            result.AuthenticationMethod!,
            NewReference());

        var properties = new AuthenticationProperties
        {
            IssuedUtc = now,
            ExpiresUtc = now.Add(_session.SlidingExpiration),
            IsPersistent = false
        };
        properties.Items["absoluteExpires"] = now.Add(_session.AbsoluteExpiration).ToString("O");
        await context.SignInAsync(AuthenticationSchemes.Session, principal, properties);
        context.User = principal;
    }

    public async Task<bool> RotateAsync(HttpContext context, string role)
    {
        var options = _options.Get(AuthenticationSchemes.Session);
        var encrypted = options.CookieManager.GetRequestCookie(context, options.Cookie.Name!);
        var outer = encrypted == null ? null : options.TicketDataFormat.Unprotect(encrypted);
        var oldKey = outer?.Principal.FindFirst(SessionIdClaim)?.Value;
        var old = oldKey == null ? null : await _store.RetrieveAsync(oldKey);
        if (old == null)
        {
            return false;
        }

        var userId = Guid.Parse(old.Principal.FindFirst(AuthenticationClaimTypes.Subject)!.Value);
        var principal = Principal(userId,
            old.Principal.FindFirst(AuthenticationClaimTypes.UserName)?.Value,
            old.Principal.FindFirst("email")?.Value,
            role,
            old.Principal.FindFirst("authMethod")!.Value,
            NewReference());

        var now = DateTimeOffset.UtcNow;
        old.Properties.IssuedUtc = now;
        old.Properties.ExpiresUtc = now.Add(_session.SlidingExpiration);
        var ticket = new AuthenticationTicket(principal, old.Properties, AuthenticationSchemes.Session);
        var newKey = await _store.RotateAsync(oldKey!, ticket);
        if (newKey == null)
        {
            return false;
        }

        WriteCookie(context, newKey, ticket);
        context.User = principal;
        return true;
    }

    private void WriteCookie(HttpContext context, string key, AuthenticationTicket ticket)
    {
        var options = _options.Get(AuthenticationSchemes.Session);
        var identity = new ClaimsIdentity([new Claim(SessionIdClaim, key)], AuthenticationSchemes.Session);
        var cookieTicket = new AuthenticationTicket(new ClaimsPrincipal(identity), ticket.Properties, AuthenticationSchemes.Session);
        var cookie = options.TicketDataFormat.Protect(cookieTicket);
        options.CookieManager.AppendResponseCookie(context, options.Cookie.Name!, cookie, options.Cookie.Build(context));
    }

    private static ClaimsPrincipal Principal(Guid userId, string? username, string? email, string role, string method, string reference)
    {
        var claims = new List<Claim>
        {
            new(AuthenticationClaimTypes.Subject, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(AuthenticationClaimTypes.ActiveRole, role),
            new(ClaimTypes.Role, role),
            new("authMethod", method),
            new(AuthenticationClaimTypes.SessionReference, reference)
        };

        if (username != null)
        {
            claims.Add(new Claim(AuthenticationClaimTypes.UserName, username));
        }

        if (email != null)
        {
            claims.Add(new Claim("email", email));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationSchemes.Session));
    }

    private static string NewReference() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
}

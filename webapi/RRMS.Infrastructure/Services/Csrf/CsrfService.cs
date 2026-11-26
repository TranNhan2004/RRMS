using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Csrf;

public sealed class CsrfService
{
    private readonly IAntiforgery _antiforgery;
    private readonly CsrfOptions _options;

    public CsrfService(IAntiforgery antiforgery, IOptions<CsrfOptions> options)
    {
        _antiforgery = antiforgery;
        _options = options.Value;
    }

    public void IssueToken(HttpContext context)
    {
        var token = _antiforgery.GetAndStoreTokens(context).RequestToken
            ?? throw new InvalidOperationException("Unable to generate CSRF request token.");
        context.Response.Cookies.Append(_options.RequestTokenCookieName, token, new CookieOptions
        {
            HttpOnly = false,
            Secure = _options.Secure,
            SameSite = _options.SameSite,
            Path = "/"
        });
        context.Response.Headers.CacheControl = "no-store";
    }

    public Task ValidateAsync(HttpContext context) => _antiforgery.ValidateRequestAsync(context);
}

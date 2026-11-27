using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Csrf;

public sealed class CsrfService
{
    private readonly IAntiforgery _antiforgery;
    private readonly CsrfOptions _options;
    private readonly IHostEnvironment _environment;

    public CsrfService(IAntiforgery antiforgery, IOptions<CsrfOptions> options, IHostEnvironment environment)
    {
        _environment = environment;
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
            Secure = CookiePolicy.IsSecure(_environment, context),
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        context.Response.Headers.CacheControl = "no-store";
    }

    public Task ValidateAsync(HttpContext context) => _antiforgery.ValidateRequestAsync(context);
}

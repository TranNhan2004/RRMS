using Microsoft.AspNetCore.Http;

namespace RRMS.Infrastructure.Options;

public class CsrfOptions
{
    public const string SectionName = "Csrf";

    public string HeaderName { get; set; } = null!;
    public string CookieTokenName { get; set; } = null!;
    public string RequestTokenCookieName { get; set; } = null!;
    public SameSiteMode SameSite { get; set; }
    public bool Secure { get; set; }
}

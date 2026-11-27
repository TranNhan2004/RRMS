namespace RRMS.Infrastructure.Options;

public class CsrfOptions
{
    public const string SectionName = "Csrf";

    public string HeaderName { get; set; } = null!;
    public string CookieTokenName { get; set; } = null!;
    public string RequestTokenCookieName { get; set; } = null!;
}

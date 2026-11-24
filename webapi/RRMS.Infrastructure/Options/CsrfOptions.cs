namespace RRMS.Infrastructure.Options;

public class CsrfOptions
{
    public const string SectionName = "Csrf";
    public string SecretKey { get; set; } = string.Empty;
    public string CookieName { get; set; } = "XSRF-TOKEN";
    public string HeaderName { get; set; } = "X-XSRF-TOKEN";
}

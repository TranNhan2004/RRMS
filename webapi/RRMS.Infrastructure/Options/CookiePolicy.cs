using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace RRMS.Infrastructure.Options;

public static class CookiePolicy
{
    public static CookieSecurePolicy SecurePolicy(IHostEnvironment environment) =>
        environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

    public static bool IsSecure(IHostEnvironment environment, HttpContext context) =>
        SecurePolicy(environment) == CookieSecurePolicy.Always || context.Request.IsHttps;
}

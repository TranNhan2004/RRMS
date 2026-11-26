using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Csrf;

namespace RRMS.Api.Extensions;

public static class CsrfExtensions
{
    public static IServiceCollection AddApplicationCsrf(this IServiceCollection services)
    {
        services.AddAntiforgery();
        services.AddOptions<AntiforgeryOptions>().Configure<IOptions<CsrfOptions>>((options, csrf) =>
        {
            options.HeaderName = csrf.Value.HeaderName;
            options.Cookie.Name = csrf.Value.CookieTokenName;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SecurePolicy = csrf.Value.Secure ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = csrf.Value.SameSite;
        });
        services.AddScoped<CsrfService>();
        return services;
    }
}

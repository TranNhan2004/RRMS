using System.Security.Claims;
using RRMS.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RRMS.Infrastructure.Services.Authentication;
using RRMS.Domain.Constants.Authentication;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Infrastructure.Options;

namespace RRMS.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? throw new InvalidOperationException("Auth settings are required.");

        services.AddRedisDataProtectionKeyStorage();
        services.AddSingleton<RedisTicketStore>();
        services.AddScoped<SessionCookieService>();
        services.AddOptions<CookieAuthenticationOptions>(AuthenticationSchemes.Session)
            .Configure<RedisTicketStore>((options, store) => options.SessionStore = store);

        services.AddOptions<CookieAuthenticationOptions>(AuthenticationSchemes.Session)
            .Configure<IHostEnvironment>((options, environment) => options.Cookie.SecurePolicy = CookiePolicy.SecurePolicy(environment));

        var authentication = services.AddAuthentication(AuthenticationSchemes.Session);

        authentication.AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName, _ => { });

        services.AddOptions<AuthenticationOptions>().Configure<IHostEnvironment>((options, environment) =>
        {
            if (environment.IsDevelopment())
            {
                options.DefaultScheme = DevelopmentAuthenticationHandler.SchemeName;
            }
        });
        authentication.AddCookie(AuthenticationSchemes.Session, options =>
        {
            options.Cookie.Name = auth.Session.Cookie.Name;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = auth.Session.SlidingExpiration;
            options.SlidingExpiration = true;
            options.Events.OnCheckSlidingExpiration = context =>
            {
                if (context.Request.Path.Value?.EndsWith("/auth/switch-role", StringComparison.OrdinalIgnoreCase) == true)
                {
                    context.ShouldRenew = false;
                }

                return Task.CompletedTask;
            };
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            };
            options.Events.OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                var role = principal?.FindFirstValue(ClaimTypes.Role);
                if (!Guid.TryParse(principal?.FindFirstValue(AuthenticationClaimTypes.Subject), out var id) ||
                    string.IsNullOrEmpty(role) ||
                    !context.Properties.Items.TryGetValue("absoluteExpires", out var expiry) ||
                    !DateTimeOffset.TryParse(expiry, out var absolute) ||
                     absolute <= DateTimeOffset.UtcNow)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(AuthenticationSchemes.Session);
                    return;
                }

                var data = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationDataService>();
                var roles = await data.FindEligibleRolesAsync(id, context.HttpContext.RequestAborted);
                if (!roles.Contains(role, StringComparer.Ordinal))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(AuthenticationSchemes.Session);
                }
            };
        });

        authentication.AddCookie(AuthenticationSchemes.External, options =>
        {
            options.Cookie.Name = "__Host-external";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = auth.Session.ExternalExpiration;
            options.SlidingExpiration = false;
        });

        authentication.AddGoogle(AuthenticationSchemes.Google, options =>
        {
            options.ClientId = auth.Google.ClientId;
            options.ClientSecret = auth.Google.ClientSecret;
            options.SignInScheme = AuthenticationSchemes.External;
            options.CallbackPath = "/signin-google";
            options.UsePkce = true;
            options.SaveTokens = false;
            options.UserInformationEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
            options.Scope.Add("openid");
            options.ClaimActions.MapJsonKey(AuthenticationClaimTypes.Subject, AuthenticationClaimTypes.Subject);
            options.ClaimActions.MapJsonKey("email", "email");
            options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            options.Events.OnRemoteFailure = context =>
            {
                context.Response.StatusCode = 401;
                context.HandleResponse();
                return Task.CompletedTask;
            };
        });
        return services;
    }
}

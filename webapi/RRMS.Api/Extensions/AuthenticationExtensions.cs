using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using RRMS.Application.Interfaces.Services.Authentication;
using Microsoft.IdentityModel.Tokens;
using RRMS.Infrastructure.Options;

namespace RRMS.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var access = options.Credentials.AccessToken;
        byte[] key;
        try
        {
            key = Convert.FromBase64String(access.SecretKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Auth:Credentials:AccessToken:SecretKey must be valid Base64.", exception);
        }

        if (key.Length < 32 || string.IsNullOrWhiteSpace(access.Issuer) || string.IsNullOrWhiteSpace(access.Audience))
        {
            throw new InvalidOperationException("A valid access token issuer, audience, and Base64 key of at least 32 bytes are required.");
        }

        var authentication = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        authentication.AddJwtBearer(bearer =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = access.Issuer,
                    ValidateAudience = true,
                    ValidAudience = access.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var claims = context.Principal;
                        if (!Guid.TryParse(claims?.FindFirst("sub")?.Value, out _) ||
                            string.IsNullOrWhiteSpace(claims?.FindFirst("jti")?.Value) ||
                            string.IsNullOrWhiteSpace(claims?.FindFirst("iat")?.Value) ||
                            string.IsNullOrWhiteSpace(claims?.FindFirst("activeRole")?.Value) ||
                            string.IsNullOrWhiteSpace(claims?.FindFirst("sessionId")?.Value) ||
                            claims?.FindFirst("tokenType")?.Value != "access")
                        {
                            context.Fail("Invalid access token claims.");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        var google = options.Google;
        if (!string.IsNullOrWhiteSpace(google.ClientId) || !string.IsNullOrWhiteSpace(google.ClientSecret))
        {
            if (string.IsNullOrWhiteSpace(google.ClientId) || string.IsNullOrWhiteSpace(google.ClientSecret))
                throw new InvalidOperationException("Google client ID and secret are both required.");
            authentication.AddCookie("GoogleExternal")
                .AddOpenIdConnect("Google", oidc =>
                {
                    oidc.Authority = "https://accounts.google.com";
                    oidc.ClientId = google.ClientId;
                    oidc.ClientSecret = google.ClientSecret;
                    oidc.SignInScheme = "GoogleExternal";
                    oidc.CallbackPath = "/login/oauth2/code/google";
                    oidc.ResponseType = "code";
                    oidc.UsePkce = true;
                    oidc.RemoteAuthenticationTimeout = google.RequestTtl;
                    oidc.SaveTokens = false;
                    oidc.MapInboundClaims = false;
                    oidc.Scope.Clear();
                    oidc.Scope.Add("openid");
                    oidc.Scope.Add("profile");
                    oidc.Scope.Add("email");
                    oidc.TokenValidationParameters.ValidIssuer = "https://accounts.google.com";
                    oidc.Events = new OpenIdConnectEvents
                    {
                        OnTicketReceived = async context =>
                        {
                            var subject = context.Principal?.FindFirst("sub")?.Value;
                            var email = context.Principal?.FindFirst("email")?.Value;
                            var verified = bool.TryParse(context.Principal?.FindFirst("email_verified")?.Value, out var value) && value;
                            try
                            {
                                if (string.IsNullOrWhiteSpace(subject)) throw new InvalidOperationException("Missing Google subject.");
                                var service = context.HttpContext.RequestServices.GetRequiredService<RRMS.Application.Interfaces.Services.Authentication.IAuthenticationService>();
                                var result = await service.GoogleLoginAsync(new RRMS.Application.Models.Authentication.GoogleLoginRequestModel { Subject = subject, Email = email, EmailVerified = verified }, context.HttpContext.RequestAborted);
                                var redirect = ValidateGoogleRedirect(result.State == "ROLE_SELECTION_REQUIRED" ? google.RoleSelectionUrl : google.CallbackUrl, google, options.Credentials.SecureCookies);
                                var response = context.HttpContext.Response;
                                response.Headers.CacheControl = "no-store";
                                if (result.State == "ROLE_SELECTION_REQUIRED" && result.RoleSelectionToken != null)
                                    response.Cookies.Append("RRMS_ROLE_SELECTION", result.RoleSelectionToken, AuthCookie(options.Credentials, true, "/api/v1/auth/login", options.Credentials.RoleSelectionTtl));
                                else if (result.State == "AUTHENTICATED" && result.RefreshToken != null && result.CsrfToken != null)
                                {
                                    var csrf = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<CsrfOptions>>().Value;
                                    response.Cookies.Append("RRMS_REFRESH", result.RefreshToken, AuthCookie(options.Credentials, true, options.Credentials.RefreshCookiePath, options.Credentials.RefreshToken.Expiration));
                                    response.Cookies.Append(csrf.CookieName, result.CsrfToken, AuthCookie(options.Credentials, false, "/", options.Credentials.RefreshToken.Expiration));
                                }
                                else throw new InvalidOperationException("Unexpected authentication state.");
                                response.Redirect(redirect);
                                context.HandleResponse();
                            }
                            catch (Exception exception) when (exception is not OperationCanceledException)
                            {
                                context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                context.HandleResponse();
                            }
                        },
                        OnRemoteFailure = context =>
                        {
                            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.HandleResponse();
                            return Task.CompletedTask;
                        }
                    };
                });
        }
        return services;
    }
    private static string ValidateGoogleRedirect(string configured, GoogleAuthOptions google, bool secureCookies)
    {
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment) || !(uri.Scheme == Uri.UriSchemeHttps || (!secureCookies && uri.Scheme == Uri.UriSchemeHttp)) ||
            !google.AllowedOrigins.Contains(uri.GetLeftPart(UriPartial.Authority), StringComparer.Ordinal))
            throw new InvalidOperationException("Invalid Google redirect configuration.");
        return uri.AbsoluteUri;
    }

    private static CookieOptions AuthCookie(CredentialAuthOptions options, bool httpOnly, string path, TimeSpan lifetime) => new()
    {
        HttpOnly = httpOnly,
        Secure = options.SecureCookies,
        SameSite = Enum.TryParse<SameSiteMode>(options.SameSite, true, out var sameSite) ? sameSite : SameSiteMode.Strict,
        Path = path,
        MaxAge = lifetime
    };
}

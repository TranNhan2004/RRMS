using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Api.Extensions;
using RRMS.Application.Models.Authentication;
using RRMS.Infrastructure.Options;
using Xunit;
using AppAuthenticationService = RRMS.Application.Interfaces.Services.Authentication.IAuthenticationService;

namespace RRMS.Api.Tests.Extensions;

public class GoogleAuthenticationTests
{
    [Fact]
    public async Task GoogleCallback_ShouldSetCookiesAndRedirect_WhenIdentityIsVerified()
    {
        var authentication = new Mock<AppAuthenticationService>();
        authentication.Setup(x => x.GoogleLoginAsync(It.Is<GoogleLoginRequestModel>(model => model.Subject == "google-subject" && model.Email == "user@example.org" && model.EmailVerified), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationResultResponseModel { State = "AUTHENTICATED", RefreshToken = "refresh", CsrfToken = "csrf" });
        using var services = CreateProvider(authentication.Object, "https://frontend.example/callback");
        var context = await CreateCallbackContext(services);
        var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Google");

        await options.Events.OnTicketReceived(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be("https://frontend.example/callback");
        context.Response.Headers.SetCookie.ToString().Should().Contain("RRMS_REFRESH=refresh").And.Contain("XSRF-TOKEN=csrf");
        options.UsePkce.Should().BeTrue();
        authentication.VerifyAll();
    }

    [Fact]
    public async Task GoogleCallback_ShouldRejectRedirect_WhenOriginIsNotAllowlisted()
    {
        var authentication = new Mock<AppAuthenticationService>();
        authentication.Setup(x => x.GoogleLoginAsync(It.IsAny<GoogleLoginRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationResultResponseModel { State = "AUTHENTICATED", RefreshToken = "refresh", CsrfToken = "csrf" });
        using var services = CreateProvider(authentication.Object, "https://other.example/callback");
        var context = await CreateCallbackContext(services);
        var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Google");

        await options.Events.OnTicketReceived(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers.SetCookie.Should().BeEmpty();
    }

    private static ServiceProvider CreateProvider(AppAuthenticationService authentication, string redirect)
    {
        var values = new Dictionary<string, string?>
        {
            ["Auth:Credentials:AccessToken:SecretKey"] = Convert.ToBase64String(new byte[32]),
            ["Auth:Google:ClientId"] = "client",
            ["Auth:Google:ClientSecret"] = "secret",
            ["Auth:Google:CallbackUrl"] = redirect,
            ["Auth:Google:AllowedOrigins:0"] = "https://frontend.example"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<CsrfOptions>(_ => { });
        services.AddSingleton(authentication);
        services.AddApplicationAuthentication(config);
        return services.BuildServiceProvider();
    }

    private static async Task<TicketReceivedContext> CreateCallbackContext(ServiceProvider services)
    {
        var http = new DefaultHttpContext { RequestServices = services };
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "google-subject"), new Claim("email", "user@example.org"), new Claim("email_verified", "true")], "Google"));
        var scheme = (await services.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync("Google"))!;
        var options = services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("Google");
        return new TicketReceivedContext(http, scheme, options, new AuthenticationTicket(principal, "Google"));
    }
}

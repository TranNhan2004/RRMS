using AwesomeAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Moq;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Csrf;
using Xunit;

namespace RRMS.Api.Tests.Middlewares;

public class CsrfServiceTests
{
    [Theory]
    [InlineData("Development", "http", false)]
    [InlineData("Development", "https", true)]
    [InlineData("Production", "http", true)]
    public void IssueToken_ShouldApplyCookiePolicy_WhenRequestTokenIsAvailable(string environmentName, string scheme, bool secure)
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(service => service.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "csrf-field", "csrf-header"));
        var service = new CsrfService(antiforgery.Object, Options.Create(new CsrfOptions
        {
            RequestTokenCookieName = "CUSTOM-XSRF"
        }), Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == environmentName));
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;

        // Act
        service.IssueToken(context);

        // Assert
        var cookie = context.Response.Headers.SetCookie.ToString();
        cookie.Should().Contain("CUSTOM-XSRF=request-token").And.Contain("path=/").And.Contain("samesite=lax");
        cookie.Contains("secure", StringComparison.OrdinalIgnoreCase).Should().Be(secure);
        cookie.Should().NotContain("httponly");
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }

    [Fact]
    public void IssueToken_ShouldThrow_WhenRequestTokenIsMissing()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(service => service.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet(null, "cookie-token", "csrf-field", "csrf-header"));
        var service = new CsrfService(antiforgery.Object, Options.Create(new CsrfOptions()), Mock.Of<IHostEnvironment>());

        // Act
        var action = () => service.IssueToken(new DefaultHttpContext());

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }
}

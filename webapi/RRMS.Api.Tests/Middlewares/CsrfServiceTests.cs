using AwesomeAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Csrf;
using Xunit;

namespace RRMS.Api.Tests.Middlewares;

public class CsrfServiceTests
{
    [Fact]
    public void IssueToken_ShouldUseConfiguredCookie_WhenRequestTokenIsAvailable()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(service => service.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "csrf-field", "csrf-header"));
        var service = new CsrfService(antiforgery.Object, Options.Create(new CsrfOptions
        {
            RequestTokenCookieName = "CUSTOM-XSRF",
            SameSite = SameSiteMode.Strict,
            Secure = true
        }));
        var context = new DefaultHttpContext();

        // Act
        service.IssueToken(context);

        // Assert
        var cookie = context.Response.Headers.SetCookie.ToString();
        cookie.Should().Contain("CUSTOM-XSRF=request-token").And.Contain("path=/").And.Contain("samesite=strict").And.Contain("secure");
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
        var service = new CsrfService(antiforgery.Object, Options.Create(new CsrfOptions()));

        // Act
        var action = () => service.IssueToken(new DefaultHttpContext());

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }
}

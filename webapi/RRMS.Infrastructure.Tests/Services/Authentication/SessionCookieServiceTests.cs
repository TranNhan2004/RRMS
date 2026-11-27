using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Constants.Authentication;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class SessionCookieServiceTests
{
    [Fact]
    public async Task SignInAsync_ShouldUseFrameworkCookieHandler_WhenSessionIsCreated()
    {
        // Arrange
        var handler = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        ClaimsPrincipal? principal = null;
        AuthenticationProperties? properties = null;
        handler.Setup(value => value.SignInAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.Session, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?>((_, _, value, options) =>
            {
                principal = value;
                properties = options;
            }).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(handler.Object);
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        var service = new SessionCookieService(null!, null!, Microsoft.Extensions.Options.Options.Create(new AuthOptions
        {
            Session = new SessionAuthOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpiration = TimeSpan.FromHours(24)
            }
        }), Mock.Of<IHostEnvironment>());
        var userId = Guid.NewGuid();

        // Act
        await service.SignInAsync(context, new AuthenticationResultResponseModel
        {
            UserId = userId,
            ActiveRole = "Tenant",
            AuthenticationMethod = "password"
        });

        // Assert
        handler.Verify(value => value.SignInAsync(context, AuthenticationSchemes.Session, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
        principal!.FindFirstValue("sub").Should().Be(userId.ToString());
        principal!.FindFirstValue(ClaimTypes.Role).Should().Be("Tenant");
        properties!.Items.Should().ContainKey("absoluteExpires");
        properties.ExpiresUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("Development", "http", false)]
    [InlineData("Development", "https", true)]
    [InlineData("Production", "http", true)]
    public void WritePending_ShouldApplyCookiePolicy_WhenPendingSessionIsCreated(string environmentName, string scheme, bool secure)
    {
        // Arrange
        var service = new SessionCookieService(null!, null!, Microsoft.Extensions.Options.Options.Create(new AuthOptions
        {
            Credentials = new CredentialAuthOptions
            {
                RoleSelectionTtl = TimeSpan.FromMinutes(5)
            },
            Session = new SessionAuthOptions(),
            PendingSession = new PendingSessionAuthOptions
            {
                Cookie = new AuthCookieOptions
                {
                    Name = "pending-session"
                }
            }
        }), Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == environmentName));
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;

        // Act
        service.WritePending(context, "token");

        // Assert
        var cookie = context.Response.Headers.SetCookie.ToString();
        cookie.Should().Contain("pending-session=token").And.Contain("httponly").And.Contain("path=/").And.Contain("samesite=lax");
        cookie.Contains("secure", StringComparison.OrdinalIgnoreCase).Should().Be(secure);
    }
}

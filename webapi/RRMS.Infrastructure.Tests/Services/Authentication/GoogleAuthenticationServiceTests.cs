using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Domain.Exceptions;
using RRMS.Domain.Constants.Authentication;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;
using IApplicationAuthenticationService = RRMS.Application.Interfaces.Services.Authentication.IAuthenticationService;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class GoogleAuthenticationServiceTests
{
    [Fact]
    public async Task PrepareLoginAsync_ShouldRejectRequest_WhenGoogleSchemeIsMissing()
    {
        // Arrange
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        var service = CreateService(schemes: schemes.Object);

        // Act
        var action = () => service.PrepareLoginAsync(CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task PrepareLoginAsync_ShouldRejectRequest_WhenRateLimitIsExceeded()
    {
        // Arrange
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        schemes.Setup(value => value.GetSchemeAsync(AuthenticationSchemes.Google))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.Google, null, typeof(TestHandler)));
        var service = CreateService(schemes: schemes.Object);

        // Act
        var action = () => service.PrepareLoginAsync(CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<RateLimitException>();
    }

    [Fact]
    public async Task BeginLinkAsync_ShouldRejectRequest_WhenLinkRateLimitIsExceeded()
    {
        // Arrange
        var user = new Mock<ICurrentUserContext>();
        user.Setup(value => value.GetRequiredUserId()).Returns(Guid.NewGuid());
        user.SetupGet(value => value.SessionReference).Returns("active-session");
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        schemes.Setup(value => value.GetSchemeAsync(AuthenticationSchemes.Google))
            .ReturnsAsync(new AuthenticationScheme(AuthenticationSchemes.Google, null, typeof(TestHandler)));
        var service = CreateService(schemes: schemes.Object, currentUser: user.Object);

        // Act
        var action = () => service.BeginLinkAsync(CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<RateLimitException>();
    }

    [Fact]
    public async Task CompleteCallbackAsync_ShouldRejectLink_WhenSessionDoesNotMatchAndClearExternalCookie()
    {
        // Arrange
        var currentUser = new Mock<ICurrentUserContext>();
        var userId = Guid.NewGuid();
        currentUser.SetupGet(value => value.IsAuthenticated).Returns(true);
        currentUser.Setup(value => value.GetRequiredUserId()).Returns(userId);
        currentUser.SetupGet(value => value.SessionReference).Returns("current-session");
        var properties = new AuthenticationProperties();
        properties.Items["linkUserId"] = userId.ToString();
        properties.Items["linkSessionRef"] = "different-session";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "google-subject")
        }, "Google"));
        var authentication = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        authentication.Setup(value => value.AuthenticateAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.External))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, AuthenticationSchemes.External)));
        authentication.Setup(value => value.SignOutAsync(It.IsAny<HttpContext>(), AuthenticationSchemes.External, It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(authentication.Object);
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        var links = new Mock<IGoogleIdentityCommandService>();
        var service = CreateService(currentUser: currentUser.Object, context: context, links: links.Object);

        // Act
        var action = () => service.CompleteCallbackAsync(CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
        links.Verify(value => value.LinkAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        authentication.Verify(value => value.SignOutAsync(context, AuthenticationSchemes.External, It.IsAny<AuthenticationProperties?>()), Times.Once);
    }

    private static GoogleAuthenticationService CreateService(IAuthenticationSchemeProvider? schemes = null, ICurrentUserContext? currentUser = null,
        HttpContext? context = null, IGoogleIdentityCommandService? links = null)
    {
        return new GoogleAuthenticationService(schemes ?? new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IAuthenticationChallengeStore>().Object, new Mock<IApplicationAuthenticationService>().Object,
            links ?? new Mock<IGoogleIdentityCommandService>().Object, null!, null!, currentUser ?? new Mock<ICurrentUserContext>().Object,
            new Mock<ICurrentRequestContext>().Object, new HttpContextAccessor
            {
                HttpContext = context
            },
            Microsoft.Extensions.Options.Options.Create(new AuthOptions
            {
                FrontendPath = "https://example.org"
            }));
    }

    private sealed class TestHandler : IAuthenticationHandler
    {
        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context) => Task.CompletedTask;
        public Task<AuthenticateResult> AuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(AuthenticationProperties? properties) => Task.CompletedTask;
    }
}

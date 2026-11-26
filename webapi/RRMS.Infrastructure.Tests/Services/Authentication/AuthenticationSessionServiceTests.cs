using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Models.Authentication;
using RRMS.Domain.Constants.Authentication;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class AuthenticationSessionServiceTests
{
    [Fact]
    public async Task CompleteAsync_ShouldRejectUnsupportedState_WhenResultIsNotRecognized()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };
        var service = new AuthenticationSessionService(null!, accessor, null!);

        // Act
        var action = () => service.CompleteAsync(new AuthenticationResultResponseModel
        {
            State = "UNKNOWN"
        }, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CompleteAsync_ShouldSetPendingCookie_WhenPendingTokenWasCreated()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };
        var options = Microsoft.Extensions.Options.Options.Create(new AuthOptions
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
                    Name = "__Host-pending",
                    Path = "/"
                }
            }
        });
        var cookies = new SessionCookieService(null!, null!, options);
        var logger = new AuthenticationEventLogger(new Mock<ICurrentRequestContext>().Object, new Mock<Serilog.ILogger>().Object);
        var service = new AuthenticationSessionService(cookies, accessor, logger);
        service.SetPendingToken("pending-token");

        // Act
        await service.CompleteAsync(new AuthenticationResultResponseModel
        {
            State = AuthenticationStates.RoleSelectionRequired
        }, CancellationToken.None);

        // Assert
        context.Response.Headers.SetCookie.ToString().Should().Contain("__Host-pending=pending-token").And.Contain("httponly").And.Contain("secure");
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        var repeat = () => service.CompleteAsync(new AuthenticationResultResponseModel
        {
            State = AuthenticationStates.RoleSelectionRequired
        }, CancellationToken.None);
        await repeat.Should().ThrowAsync<InvalidOperationException>();
    }
}

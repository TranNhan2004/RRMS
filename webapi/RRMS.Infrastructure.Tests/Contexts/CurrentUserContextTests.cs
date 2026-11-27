using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Contexts;
using Xunit;

namespace RRMS.Infrastructure.Tests.Contexts;

public class CurrentUserContextTests
{
    [Fact]
    public void UserId_ShouldReturnSubject_WhenPrincipalIsAuthenticated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Bearer"));
        var currentUser = new CurrentUserContext(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        // Act
        var result = currentUser.UserId;

        // Assert
        result.Should().Be(id);
    }

    [Fact]
    public void UserId_ShouldReflectCurrentPrincipal_WhenIdentityChanges()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var currentUser = new CurrentUserContext(new HttpContextAccessor
        {
            HttpContext = context
        });

        // Act
        var initialUserId = currentUser.UserId;
        var id = Guid.NewGuid();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Session"));
        var authenticatedUserId = currentUser.GetRequiredUserId();

        // Assert
        initialUserId.Should().BeNull();
        authenticatedUserId.Should().Be(id);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("invalid")]
    public void GetRequiredUserId_ShouldRejectIdentifier_WhenSubjectIsInvalid(string subject)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "Session"));
        var currentUser = new CurrentUserContext(new HttpContextAccessor
        {
            HttpContext = context
        });

        // Act
        var action = () => currentUser.GetRequiredUserId();

        // Assert
        action.Should().Throw<UnauthorizedException>();
    }

    [Fact]
    public void UserId_ShouldRejectSubject_WhenPrincipalIsAnonymous()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())]));
        var currentUser = new CurrentUserContext(new HttpContextAccessor
        {
            HttpContext = httpContext
        });

        // Act
        var userId = currentUser.UserId;
        var action = () => currentUser.GetRequiredUserId();

        // Assert
        userId.Should().BeNull();
        action.Should().Throw<UnauthorizedException>();
    }
}

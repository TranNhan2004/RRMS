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
        var id = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Bearer"));
        var currentUser = new CurrentUserContext(new HttpContextAccessor { HttpContext = httpContext });

        var result = currentUser.UserId;

        result.Should().Be(id);
    }

    [Fact]
    public void UserId_ShouldRejectSubject_WhenPrincipalIsAnonymous()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())]));
        var currentUser = new CurrentUserContext(new HttpContextAccessor { HttpContext = httpContext });

        var action = () => currentUser.UserId;

        action.Should().Throw<UnauthorizedException>();
    }
}

using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using RRMS.Api.Middlewares;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Infrastructure.Options;
using Xunit;

namespace RRMS.Api.Tests.Middlewares;

public class CsrfMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldRejectRefresh_WhenCsrfIsMissing()
    {
        var tokens = new Mock<ITokenService>();
        var reached = false;
        var middleware = new CsrfMiddleware(_ => { reached = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/auth/refresh";

        await middleware.InvokeAsync(context, tokens.Object, Microsoft.Extensions.Options.Options.Create(new CsrfOptions()));

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        reached.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowRequest_WhenCsrfIsBoundToRefreshSession()
    {
        var tokens = new Mock<ITokenService>();
        tokens.Setup(x => x.ValidateRefreshToken("refresh")).Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("jti", "session")])));
        tokens.Setup(x => x.ValidateCsrfToken("session", "csrf", "csrf")).Returns(true);
        var reached = false;
        var middleware = new CsrfMiddleware(_ => { reached = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/auth/refresh";
        context.Request.Headers.Cookie = "RRMS_REFRESH=refresh; XSRF-TOKEN=csrf";
        context.Request.Headers["X-XSRF-TOKEN"] = "csrf";

        await middleware.InvokeAsync(context, tokens.Object, Microsoft.Extensions.Options.Options.Create(new CsrfOptions()));

        reached.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowLogin_WhenCsrfIsAbsent()
    {
        var reached = false;
        var middleware = new CsrfMiddleware(_ => { reached = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/auth/login";

        await middleware.InvokeAsync(context, new Mock<ITokenService>().Object, Microsoft.Extensions.Options.Options.Create(new CsrfOptions()));

        reached.Should().BeTrue();
    }
}

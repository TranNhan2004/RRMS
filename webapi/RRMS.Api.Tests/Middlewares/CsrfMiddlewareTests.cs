using AwesomeAssertions;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Api.Attributes;
using RRMS.Api.Middlewares;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Csrf;
using Xunit;

namespace RRMS.Api.Tests.Middlewares;

public class CsrfMiddlewareTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task InvokeAsync_ShouldRejectUnsafeRequest_WhenAntiforgeryValidationFails(string method)
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).ThrowsAsync(new AntiforgeryValidationException("invalid"));
        var reached = false;
        var middleware = new CsrfMiddleware(_ => { reached = true; return Task.CompletedTask; });
        var context = NewContext(method);
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, NewService(antiforgery.Object));

        // Assert
        context.Response.StatusCode.Should().Be(400);
        context.Response.Body.Position = 0;
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().Contain("CSRF_VALIDATION_FAILED").And.NotContain("invalid\"");
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("message").GetString().Should().Be("Invalid or missing antiforgery token.");
        document.RootElement.GetProperty("details").GetProperty("code").GetString().Should().Be("CSRF_VALIDATION_FAILED");
        reached.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ShouldValidate_WhenAnonymousEndpointUsesUnsafeMethod()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(value => value.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("invalid"));
        var middleware = new CsrfMiddleware(_ => Task.CompletedTask);
        var context = NewContext("POST");
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "anonymous"));
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, NewService(antiforgery.Object));

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        antiforgery.Verify(value => value.ValidateRequestAsync(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSkipValidation_WhenEndpointIsMissing()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        var middleware = new CsrfMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";

        // Act
        await middleware.InvokeAsync(context, NewService(antiforgery.Object));

        // Assert
        antiforgery.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldContinue_WhenAntiforgeryIsValid()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>();
        var reached = false;
        var middleware = new CsrfMiddleware(_ => { reached = true; return Task.CompletedTask; });
        var context = NewContext("POST");

        // Act
        await middleware.InvokeAsync(context, NewService(antiforgery.Object));

        // Assert
        reached.Should().BeTrue();
        antiforgery.Verify(x => x.ValidateRequestAsync(context), Times.Once);
    }

    [Theory]
    [InlineData("GET", false)]
    [InlineData("POST", true)]
    public async Task InvokeAsync_ShouldSkipValidation_WhenRequestIsSafeOrExplicitlyExempt(string method, bool skip)
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        var middleware = new CsrfMiddleware(_ => Task.CompletedTask);
        var context = NewContext(method, skip);

        // Act
        await middleware.InvokeAsync(context, NewService(antiforgery.Object));

        // Assert
        antiforgery.VerifyNoOtherCalls();
    }

    private static CsrfService NewService(IAntiforgery antiforgery) => new(antiforgery, Options.Create(new CsrfOptions()), Mock.Of<Microsoft.Extensions.Hosting.IHostEnvironment>());

    private static DefaultHttpContext NewContext(string method, bool skip = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(skip ? new object[] { new SkipCsrfAttribute() } : Array.Empty<object>()), "test"));
        return context;
    }
}

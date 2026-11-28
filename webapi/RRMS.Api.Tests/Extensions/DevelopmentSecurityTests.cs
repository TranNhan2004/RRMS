using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Csrf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RRMS.Api.Authentication;
using RRMS.Api.Extensions;
using RRMS.Domain.Constants.Authentication;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class DevelopmentSecurityTests
{
    [Theory]
    [InlineData("Development", DevelopmentAuthenticationHandler.SchemeName)]
    [InlineData("Production", AuthenticationSchemes.Session)]
    [InlineData("Dev", AuthenticationSchemes.Session)]
    public async Task AddApplicationAuthentication_ShouldChooseScheme_WhenEnvironmentIsConfigured(string environmentName, string expectedScheme)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == environmentName));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Session:ExternalExpiration"] = "00:05:00"
        }).Build();
        services.AddApplicationAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var scheme = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync();

        // Assert
        scheme!.Name.Should().Be(expectedScheme);
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    [InlineData("Dev", false)]
    public async Task DevelopmentAuthenticationHandler_ShouldAuthenticateOnly_WhenEnvironmentIsDevelopment(string environmentName, bool expectedSuccess)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == environmentName));
        services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(DevelopmentAuthenticationHandler.SchemeName, _ => { });
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };

        // Act
        var result = await context.AuthenticateAsync(DevelopmentAuthenticationHandler.SchemeName);

        // Assert
        result.Succeeded.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
        }
    }

    [Fact]
    public async Task UseApplicationCsrf_ShouldSkipMiddleware_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        var environment = Mock.Of<IHostEnvironment>(value => value.EnvironmentName == "Development");
        app.UseApplicationCsrf(environment);
        app.Run(context => { context.Response.StatusCode = StatusCodes.Status204NoContent; return Task.CompletedTask; });
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = "POST";

        // Act
        await app.Build()(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }
    [Theory]
    [InlineData("Production")]
    [InlineData("Dev")]
    public async Task UseApplicationCsrf_ShouldRejectInvalidToken_WhenEnvironmentIsNotDevelopment(string environmentName)
    {
        // Arrange
        var environment = Mock.Of<IHostEnvironment>(value => value.EnvironmentName == environmentName);
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(value => value.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("invalid"));
        var services = new ServiceCollection();
        services.AddSingleton(new CsrfService(antiforgery.Object, Options.Create(new CsrfOptions()), environment));
        using var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseApplicationCsrf(environment);
        app.Run(context => { context.Response.StatusCode = StatusCodes.Status204NoContent; return Task.CompletedTask; });
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = "POST";
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "test"));

        // Act
        await app.Build()(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        antiforgery.Verify(value => value.ValidateRequestAsync(context), Times.Once);
    }
}

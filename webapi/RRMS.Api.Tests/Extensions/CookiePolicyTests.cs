using AwesomeAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Api.Extensions;
using RRMS.Infrastructure.Options;
using StackExchange.Redis;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class CookiePolicyTests
{
    [Theory]
    [InlineData("Development", CookieSecurePolicy.SameAsRequest)]
    [InlineData("Production", CookieSecurePolicy.Always)]
    [InlineData("Staging", CookieSecurePolicy.Always)]
    public void CookieOptions_ShouldUseEnvironmentPolicy_WhenConfigured(string environmentName, CookieSecurePolicy expected)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Session:Cookie:Name"] = "session",
            ["Auth:Session:SlidingExpiration"] = "00:30:00",
            ["Auth:Session:ExternalExpiration"] = "00:05:00",
            ["Auth:Google:ClientId"] = "client",
            ["Auth:Google:ClientSecret"] = "secret"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == environmentName));
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(Mock.Of<IDatabase>());
        services.AddSingleton(redis.Object);
        services.AddApplicationAuthentication(configuration);
        services.Configure<CsrfOptions>(options =>
        {
            options.CookieTokenName = "antiforgery";
            options.HeaderName = "X-XSRF-TOKEN";
        });
        services.AddApplicationCsrf();
        using var provider = services.BuildServiceProvider();

        // Act
        var session = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Session");
        var csrf = provider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        // Assert
        session.Cookie.SecurePolicy.Should().Be(expected);
        session.Cookie.SameSite.Should().Be(SameSiteMode.Lax);
        session.Cookie.Path.Should().Be("/");
        csrf.Cookie.SecurePolicy.Should().Be(expected);
        csrf.Cookie.SameSite.Should().Be(SameSiteMode.Lax);
        csrf.Cookie.Path.Should().Be("/");
    }
}

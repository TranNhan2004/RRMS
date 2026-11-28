using System.Net;
using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RRMS.Domain.Constants.Policies;
using RRMS.Api.Extensions;
using RRMS.Infrastructure;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class RateLimiterExtensionsTests
{
    [Fact]
    public async Task AddApplicationRateLimiter_ShouldRejectExcessRequests_WhenAuthIpExceedsLimit()
    {
        // Arrange
        using var provider = CreateServices();
        var app = new ApplicationBuilder(provider);
        app.UseRateLimiter();
        app.Run(context => { context.Response.StatusCode = StatusCodes.Status200OK; return Task.CompletedTask; });
        var pipeline = app.Build();
        var first = CreateContext(provider, RateLimiterPolicies.Auth, "192.0.2.1");
        var second = CreateContext(provider, RateLimiterPolicies.Auth, "192.0.2.1");
        var other = CreateContext(provider, RateLimiterPolicies.Auth, "192.0.2.2");

        // Act
        await pipeline(first);
        await pipeline(second);
        await pipeline(other);

        // Assert
        first.Response.StatusCode.Should().Be(200);
        second.Response.StatusCode.Should().Be(429);
        other.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AddApplicationRateLimiter_ShouldPartitionByUser_WhenRequestsAreAuthenticated()
    {
        // Arrange
        using var provider = CreateServices();
        var app = new ApplicationBuilder(provider);
        app.UseRateLimiter();
        app.Run(context => { context.Response.StatusCode = StatusCodes.Status200OK; return Task.CompletedTask; });
        var pipeline = app.Build();
        var first = CreateContext(provider, RateLimiterPolicies.Standard, "192.0.2.1", "user-one");
        var second = CreateContext(provider, RateLimiterPolicies.Standard, "192.0.2.2", "user-one");
        var other = CreateContext(provider, RateLimiterPolicies.Standard, "192.0.2.1", "user-two");

        // Act
        await pipeline(first);
        await pipeline(second);
        await pipeline(other);

        // Assert
        first.Response.StatusCode.Should().Be(200);
        second.Response.StatusCode.Should().Be(429);
        other.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public void AddApplicationRateLimiter_ShouldRejectInvalidConfiguration_WhenWindowIsTooShortForSegments()
    {
        // Arrange
        using var provider = CreateServices("00:00:00.0000001", "2");

        // Act
        var action = () => provider.GetRequiredService<IOptions<RRMS.Infrastructure.Options.RateLimiterOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    private static ServiceProvider CreateServices(string window = "00:01:00", string segments = "1")
    {
        var values = new Dictionary<string, string?>();
        foreach (var policy in new[] { "Auth", "Standard", "Export" })
        {
            values[$"RateLimiter:{policy}:PermitLimit"] = "1";
            values[$"RateLimiter:{policy}:Window"] = window;
            values[$"RateLimiter:{policy}:SegmentsPerWindow"] = segments;
            values[$"RateLimiter:{policy}:QueueLimit"] = "0";
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplicationOptions(configuration);
        services.AddApplicationRateLimiter(configuration);
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider provider, string policy, string ip, string? user = null)
    {
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(policy)), "test"));
        if (user != null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user)], "test"));
        }
        return context;
    }
}

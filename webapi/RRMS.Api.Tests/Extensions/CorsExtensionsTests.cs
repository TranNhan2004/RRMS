using AwesomeAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RRMS.Api.Extensions;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class CorsExtensionsTests
{
    [Fact]
    public async Task AddApplicationCors_ShouldAllowConfiguredOrigin_WhenPreflightIsAllowed()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://example.org"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationCors(configuration);
        using var provider = services.BuildServiceProvider();
        var policies = provider.GetRequiredService<ICorsPolicyProvider>();
        var context = new DefaultHttpContext();
        context.Request.Headers.Origin = "https://example.org";
        context.Request.Method = "OPTIONS";
        context.Request.Headers.AccessControlRequestMethod = "POST";
        context.Request.Headers.AccessControlRequestHeaders = "content-type,x-xsrf-token";

        var policy = await policies.GetPolicyAsync(context, CorsExtensions.PolicyName);
        var result = provider.GetRequiredService<ICorsService>().EvaluatePolicy(context, policy!);

        result.IsOriginAllowed.Should().BeTrue();
        result.SupportsCredentials.Should().BeTrue();
        result.AllowedHeaders.Should().Contain(header => string.Equals(header, "x-xsrf-token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddApplicationCors_ShouldRejectWildcard_WhenCredentialsAreAllowed()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "*"
        }).Build();
        var services = new ServiceCollection();

        var action = () => services.AddApplicationCors(configuration);

        action.Should().Throw<InvalidOperationException>();
    }
}

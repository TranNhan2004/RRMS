using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RRMS.Api.Extensions;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class ForwardedHeadersExtensionsTests
{
    [Fact]
    public void AddTrustedForwardedHeaders_ShouldTrustConfiguredProxy_WhenAddressIsValid()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "192.0.2.1"
        }).Build();
        var services = new ServiceCollection();
        services.AddTrustedForwardedHeaders(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        // Assert
        options.KnownProxies.Should().Contain(IPAddress.Parse("192.0.2.1"));
        options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
    }

    [Fact]
    public void AddTrustedForwardedHeaders_ShouldRejectConfiguration_WhenProxyAddressIsInvalid()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "not-an-ip"
        }).Build();

        // Act
        var action = () => new ServiceCollection().AddTrustedForwardedHeaders(configuration);

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }
    [Fact]
    public void AddTrustedForwardedHeaders_ShouldTrustConfiguredNetwork_WhenCidrIsValid()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = "192.0.2.0/24"
        }).Build();
        var services = new ServiceCollection();
        services.AddTrustedForwardedHeaders(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        // Assert
        options.KnownIPNetworks.Should().Contain(System.Net.IPNetwork.Parse("192.0.2.0/24"));
    }

    [Theory]
    [InlineData("192.0.2.0/33")]
    [InlineData("invalid")]
    public void AddTrustedForwardedHeaders_ShouldRejectConfiguration_WhenNetworkIsInvalid(string network)
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = network
        }).Build();

        // Act
        var action = () => new ServiceCollection().AddTrustedForwardedHeaders(configuration);

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }
}

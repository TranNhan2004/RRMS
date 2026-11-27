using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RRMS.Api.Extensions;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class AuthorizationExtensionsTests
{
    [Fact]
    public void AddApplicationAuthorization_ShouldRequireAuthenticatedUser_WhenEndpointHasNoPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationAuthorization();
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert
        options.FallbackPolicy.Should().NotBeNull();
        options.FallbackPolicy!.Requirements.Should().ContainSingle(requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }
}

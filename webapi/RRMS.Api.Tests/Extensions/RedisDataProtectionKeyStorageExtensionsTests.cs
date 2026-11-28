using AwesomeAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RRMS.Api.Extensions;
using StackExchange.Redis;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class RedisDataProtectionKeyStorageExtensionsTests
{
    [Fact]
    public void AddRedisDataProtectionKeyStorage_ShouldConfigureRedisRepository_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IConnectionMultiplexer>());

        // Act
        services.AddRedisDataProtectionKeyStorage();
        using var provider = services.BuildServiceProvider();
        var keys = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        var protection = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

        // Assert
        keys.XmlRepository.Should().BeOfType<RedisXmlRepository>();
        protection.ApplicationDiscriminator.Should().Be("RRMS");
    }
}

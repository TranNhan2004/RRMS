using AwesomeAssertions;
using Moq;
using RRMS.Domain.Constants.CacheKeys;
using RRMS.Infrastructure.Services.Cache;
using StackExchange.Redis;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Cache;

public class RedisServiceTests
{
    [Fact]
    public async Task SetAsync_ShouldStoreWebFormattedJson_WhenValueIsProvided()
    {
        // Arrange
        var database = new Mock<IDatabase>();
        RedisValue storedValue = default;
        database.Setup(value => value.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>(), It.IsAny<ValueCondition>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, Expiration, ValueCondition, CommandFlags>((_, value, _, _, _) => storedValue = value)
            .ReturnsAsync(true);
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        var service = new RedisService(connection.Object);

        // Act
        await service.SetAsync(new CacheKeyObject
        {
            Key = "user"
        }, new CacheValue
        {
            DisplayName = "Tenant",
            Count = 2
        });

        // Assert
        storedValue.ToString().Should().Be("{\"displayName\":\"Tenant\",\"count\":2}");
    }

    [Fact]
    public async Task GetAsync_ShouldReadWebFormattedJson_WhenCachedValueExists()
    {
        // Arrange
        var database = new Mock<IDatabase>();
        database.Setup(value => value.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"{\"DISPLAYNAME\":\"Tenant\",\"count\":\"2\"}");
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(value => value.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        var service = new RedisService(connection.Object);

        // Act
        var result = await service.GetAsync<CacheValue>(new CacheKeyObject
        {
            Key = "user"
        });

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Tenant");
        result.Count.Should().Be(2);
    }

    private sealed class CacheValue
    {
        public string DisplayName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

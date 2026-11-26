using AwesomeAssertions;
using Moq;
using RRMS.Infrastructure.Services.Authentication;
using StackExchange.Redis;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class AuthenticationChallengeStoreTests
{
    [Fact]
    public async Task FindAsync_ShouldReturnNull_WhenTokenHasInvalidShape()
    {
        // Arrange
        var database = new Mock<IDatabase>(MockBehavior.Strict);
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        var store = new AuthenticationChallengeStore(redis.Object);

        // Act
        var result = await store.FindAsync("role-selection", "invalid");

        // Assert
        result.Should().BeNull();
        database.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AllowAsync_ShouldRejectInvalidLimit_WhenLimitIsZero()
    {
        // Arrange
        var database = new Mock<IDatabase>(MockBehavior.Strict);
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        var store = new AuthenticationChallengeStore(redis.Object);

        // Act
        var action = () => store.AllowAsync("login-ip", "127.0.0.1", 0, TimeSpan.FromMinutes(1));

        // Assert
        await action.Should().ThrowAsync<ArgumentException>();
        database.VerifyNoOtherCalls();
    }
}

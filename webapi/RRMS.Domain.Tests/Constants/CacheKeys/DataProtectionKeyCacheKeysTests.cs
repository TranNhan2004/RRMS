using AwesomeAssertions;
using RRMS.Domain.Constants.CacheKeys;
using Xunit;

namespace RRMS.Domain.Tests.Constants.CacheKeys;

public class DataProtectionKeyCacheKeysTests
{
    [Fact]
    public void GetDpkRingKey_ShouldReturnPersistentKey_WhenCalled()
    {
        // Arrange

        // Act
        var key = DataProtectionKeyCacheKeys.GetDpkRingKey();

        // Assert
        key.Key.Should().Be("rrms:data-protection-keys");
        key.Expiration.Should().BeNull();
    }
}

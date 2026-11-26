using AwesomeAssertions;
using RRMS.Infrastructure.Services.Authentication;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Authentication;

public class Argon2PasswordServiceTests
{
    [Fact]
    public void Verify_ShouldAcceptJavaCompatibleHash_WhenPasswordMatches()
    {
        // Arrange
        const string hash = "{argon2}$argon2id$v=19$m=19456,t=2,p=1$MDEyMzQ1Njc4OWFiY2RlZg$9CWXp0XaAuMQPZzNccREnUDxhapXcY1ekEZi8T8qqxw";
        var service = new Argon2PasswordService();

        // Act
        var result = service.Verify("example-password", hash);

        // Assert
        result.Should().BeTrue();
        service.Verify("incorrect-password", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_ShouldGenerateDistinctValidHashes_WhenPasswordIsRepeated()
    {
        // Arrange
        var service = new Argon2PasswordService();

        // Act
        var first = service.Hash("example-password");
        var second = service.Hash("example-password");

        // Assert
        first.Should().NotBe(second);
        service.Verify("example-password", first).Should().BeTrue();
        service.Verify("example-password", second).Should().BeTrue();
    }
}

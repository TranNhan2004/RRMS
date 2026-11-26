using RRMS.Application.Interfaces.Services.Email;
using AwesomeAssertions;
using Moq;
using RRMS.Application.Models.Email;
using RRMS.Infrastructure.Services.Authentication;
using RRMS.Infrastructure.Services.Email;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Email;

public class AuthenticationEmailServiceTests
{
    [Fact]
    public async Task SendVerificationAsync_ShouldQueueEncodedHtml_WhenConfigurationIsValid()
    {
        // Arrange
        var queue = new Mock<IEmailQueueService>();
        EmailMessageModel? email = null;
        queue.Setup(x => x.TryEnqueue(It.IsAny<EmailMessageModel>())).Callback<EmailMessageModel>(value => email = value).Returns(true);
        var service = new AuthenticationEmailService(queue.Object);

        // Act
        await service.SendVerificationAsync("user@example.org", "https://example.org/?a=1&b=2\" onclick=\"bad");

        // Assert
        email.Should().NotBeNull();
        email!.Subject.Should().Be("Verify your email");
        email.HtmlBody.Should().Contain("&amp;").And.Contain("&quot;").And.NotContain("\" onclick=\"");
    }

    [Fact]
    public async Task SendVerificationAsync_ShouldFail_WhenQueueIsFull()
    {
        // Arrange
        var service = new AuthenticationEmailService(new Mock<IEmailQueueService>().Object);

        // Act
        var action = () => service.SendVerificationAsync("user@example.org", "https://example.org/verify");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}

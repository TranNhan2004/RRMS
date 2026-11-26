using AwesomeAssertions;
using Microsoft.Extensions.Options;
using RRMS.Application.Models.Email;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Email;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Email;

public class EmailQueueServiceTests
{
    [Fact]
    public async Task TryEnqueue_ShouldPreserveMessages_WhenQueueHasCapacity()
    {
        // Arrange
        var queue = CreateQueue(2);
        var message = Message();
        queue.TryEnqueue(message).Should().BeTrue();
        await using var reader = queue.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();

        // Act
        var hasNext = await reader.MoveNextAsync();

        // Assert
        hasNext.Should().BeTrue();
        reader.Current.Should().BeSameAs(message);
    }

    [Fact]
    public void TryEnqueue_ShouldReturnFalse_WhenQueueIsFull()
    {
        // Arrange
        var queue = CreateQueue(1);
        queue.TryEnqueue(Message()).Should().BeTrue();

        // Act
        var result = queue.TryEnqueue(Message());

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("invalid", "Subject", "<p>Text</p>")]
    [InlineData("user@example.org", "Subject\r\nBcc: evil@example.org", "<p>Text</p>")]
    [InlineData("user@example.org", "Subject", "")]
    public void TryEnqueue_ShouldRejectInvalidMessage_WhenFieldsAreInvalid(string recipient, string subject, string body)
    {
        // Arrange
        var queue = CreateQueue(1);
        var message = new EmailMessageModel
        {
            Recipient = recipient,
            Subject = subject,
            HtmlBody = body
        };

        // Act
        var action = () => queue.TryEnqueue(message);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    private static EmailQueueService CreateQueue(int capacity) => new(Microsoft.Extensions.Options.Options.Create(new EmailOptions
    {
        QueueCapacity = capacity
    }));
    private static EmailMessageModel Message() => new()
    {
        Recipient = "user@example.org",
        Subject = "Subject",
        HtmlBody = "<p>Text</p>"
    };
}

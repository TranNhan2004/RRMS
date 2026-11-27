using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using RRMS.Application.Models.Email;
using RRMS.Application.Interfaces.Services.Email;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Services.Email;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.Email;

public class SmtpEmailTests
{
    [Fact]
    public void CreateMessage_ShouldSetHtmlBody_WhenEmailIsPrepared()
    {
        // Arrange
        var sender = new SmtpEmailSender(Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            DefaultFromEmail = "sender@example.org",
            DefaultFromName = "Rentals"
        }), new Mock<IHostEnvironment>().Object);

        // Act
        var message = sender.CreateMessage(new EmailMessageModel
        {
            Recipient = "user@example.org",
            Subject = "Verify",
            HtmlBody = "<p>Hello</p>"
        });

        // Assert
        message.Subject.Should().Be("Verify");
        message.From.Mailboxes.Single().Address.Should().Be("sender@example.org");
        message.Body.Should().BeOfType<TextPart>().Which.IsHtml.Should().BeTrue();
        message.HtmlBody.Should().Be("<p>Hello</p>");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueProcessing_WhenSendingAnEmailFails()
    {
        // Arrange
        var queue = new EmailQueueService(Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            QueueCapacity = 2
        }));
        var sender = new Mock<IEmailSenderService>();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sender.SetupSequence(x => x.SendAsync(It.IsAny<EmailMessageModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("private"))
            .Returns(() => { completed.SetResult(); return Task.CompletedTask; });
        var logger = new Mock<Serilog.ILogger>();
        using var service = new SmtpEmailBackgroundService(queue, sender.Object, logger.Object);
        var message = new EmailMessageModel
        {
            Recipient = "user@example.org",
            Subject = "Subject",
            HtmlBody = "<p>Body</p>"
        };
        queue.TryEnqueue(message).Should().BeTrue();
        queue.TryEnqueue(message).Should().BeTrue();

        // Act
        await service.StartAsync(CancellationToken.None);
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        // Assert
        sender.Verify(x => x.SendAsync(message, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

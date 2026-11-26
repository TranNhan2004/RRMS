using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RRMS.Application.Interfaces.Services.Exports;
using RRMS.Application.Models.Exports;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Services.ExportJobs;
using Xunit;

namespace RRMS.Infrastructure.Tests.Services.ExportJobs;

public class ExportBackgroundServiceTests
{
    [Fact]
    public async Task ProcessAsync_ShouldDispatchJob_WhenExactlyOneHandlerMatches()
    {
        // Arrange
        var job = new ExportJobModel
        {
            JobId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FileName = "a",
            Data = new object()
        };
        var handler = new Mock<IExportJobHandler>();
        handler.Setup(x => x.CanHandle(job)).Returns(true);
        handler.Setup(x => x.HandleAsync(job, CancellationToken.None)).ReturnsAsync(new ExportJobResultModel
        {
            FileId = Guid.NewGuid()
        });
        using var provider = new ServiceCollection().AddScoped(_ => handler.Object).BuildServiceProvider();
        var queue = new Mock<IExportQueueService>();
        var logger = new Mock<Serilog.ILogger>();
        var service = new ExportBackgroundService(queue.Object, provider.GetRequiredService<IServiceScopeFactory>(), logger.Object);

        // Act
        await service.ProcessAsync(job, CancellationToken.None);

        // Assert
        handler.Verify(x => x.HandleAsync(job, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRejectJob_WhenNoHandlerMatches()
    {
        // Arrange
        var job = new ExportJobModel
        {
            JobId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            FileName = "a",
            Data = new object()
        };
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = new ExportBackgroundService(new Mock<IExportQueueService>().Object, provider.GetRequiredService<IServiceScopeFactory>(), new Mock<Serilog.ILogger>().Object);

        // Act
        var action = () => service.ProcessAsync(job, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<UnprocessableEntityException>();
    }
}

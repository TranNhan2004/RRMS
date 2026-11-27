using Microsoft.Extensions.Hosting;
using RRMS.Application.Interfaces.Services.Email;

namespace RRMS.Infrastructure.Services.Email;

public sealed class SmtpEmailBackgroundService : BackgroundService
{
    private readonly IEmailQueueService _queue;
    private readonly IEmailSenderService _sender;
    private readonly Serilog.ILogger _logger;

    public SmtpEmailBackgroundService(
        IEmailQueueService queue,
        IEmailSenderService sender,
        Serilog.ILogger logger)
    {
        _queue = queue;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _sender.SendAsync(message, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.Error("Email delivery failed with {ErrorType}", exception.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}

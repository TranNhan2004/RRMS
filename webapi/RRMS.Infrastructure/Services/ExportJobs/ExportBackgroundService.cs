using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RRMS.Application.Interfaces.Services.Exports;
using RRMS.Application.Models.Exports;
using RRMS.Domain.Exceptions;

namespace RRMS.Infrastructure.Services.ExportJobs;

public sealed class ExportBackgroundService : BackgroundService
{
    private readonly IExportQueueService _exportQueueService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Serilog.ILogger _logger;

    public ExportBackgroundService(
        IExportQueueService exportQueueService,
        IServiceScopeFactory serviceScopeFactory,
        Serilog.ILogger logger)
    {
        _exportQueueService = exportQueueService;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _exportQueueService.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Export job {JobId} could not be processed", job.JobId);
            }
        }
    }

    internal async Task ProcessAsync(ExportJobModel job, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider
            .GetServices<IExportJobHandler>()
            .Where(handler => handler.CanHandle(job))
            .Take(2)
            .ToList();

        if (handlers.Count != 1)
        {
            throw new UnprocessableEntityException("An export job must have exactly one matching handler.");
        }

        var result = await handlers[0].HandleAsync(job, cancellationToken);
        _logger.Information("Export job {JobId} completed with file {FileId}", job.JobId, result.FileId);
    }
}

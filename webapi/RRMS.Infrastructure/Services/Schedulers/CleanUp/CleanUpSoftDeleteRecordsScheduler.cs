using Microsoft.Extensions.Hosting;

namespace RRMS.Infrastructure.Services.Schedulers.CleanUp;

public class CleanUpSoftDeleteRecordsScheduler : BackgroundService
{
    public CleanUpSoftDeleteRecordsScheduler()
    {
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
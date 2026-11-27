using System.Threading.Channels;
using Microsoft.Extensions.Options;
using RRMS.Application.Interfaces.Services.Exports;
using RRMS.Application.Models.Exports;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.ExportJobs;

public sealed class ExportQueueService : IExportQueueService
{
    private readonly Channel<ExportJobModel> _queue;
    private readonly ExportOptions _exportOptions;

    public ExportQueueService(IOptions<ExportOptions> exportOptions)
    {
        _exportOptions = exportOptions.Value;
        _queue = Channel.CreateBounded<ExportJobModel>(new BoundedChannelOptions(_exportOptions.MaxQueueItems)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public bool TryEnqueue(ExportJobModel job)
        => _queue.Writer.TryWrite(job);

    public IAsyncEnumerable<ExportJobModel> ReadAllAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAllAsync(cancellationToken);
}

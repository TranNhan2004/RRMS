using RRMS.Application.Models.Exports;

namespace RRMS.Application.Interfaces.Services.Exports;

public interface IExportQueueService
{
    bool TryEnqueue(ExportJobModel job);
    IAsyncEnumerable<ExportJobModel> ReadAllAsync(CancellationToken cancellationToken);
}

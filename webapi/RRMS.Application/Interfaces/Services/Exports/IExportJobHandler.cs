using RRMS.Application.Models.Exports;

namespace RRMS.Application.Interfaces.Services.Exports;

public interface IExportJobHandler
{
    bool CanHandle(ExportJobModel job);
    Task<ExportJobResultModel> HandleAsync(ExportJobModel job, CancellationToken cancellationToken = default);
}

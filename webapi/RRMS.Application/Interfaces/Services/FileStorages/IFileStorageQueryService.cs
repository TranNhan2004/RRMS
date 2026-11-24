using RRMS.Application.Models.FileStorages;

namespace RRMS.Application.Interfaces.Services.FileStorages;

public interface IFileStorageQueryService
{
    Task<string> GetPresignedDownloadUrlAsync(GetFileUrlRequestModel model, CancellationToken cancellationToken = default);
    Task<string> GetPresignedViewUrlAsync(GetFileUrlRequestModel model, CancellationToken cancellationToken = default);
}

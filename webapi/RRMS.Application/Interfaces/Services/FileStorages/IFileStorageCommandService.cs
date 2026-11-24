using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Services.FileStorages;

public interface IFileStorageCommandService
{
    Task<FileStorage> CreateFileStorageAsync(Stream content, FileStorage fileStorage, CancellationToken cancellationToken = default);
}

using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;

namespace RRMS.Infrastructure.Repositories;

public class FileStorageRepository : GenericRepository<FileStorage>, IFileStorageRepository
{
    public FileStorageRepository(Serilog.ILogger logger, AppDbContext appDbContext)
        : base(logger, appDbContext)
    {
    }
}

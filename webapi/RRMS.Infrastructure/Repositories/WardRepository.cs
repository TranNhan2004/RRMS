using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;
using RRMS.Infrastructure.Persistence.Extensions;

namespace RRMS.Infrastructure.Repositories;

public class WardRepository : GenericRepository<Ward>, IWardRepository
{
    public WardRepository(Serilog.ILogger logger, AppDbContext context) : base(logger, context) { }

    public Task<FilterResult<GetFilteredWardResponseItemModel>> FindFilteredWardsAsync(GetFilteredWardsRequestModel request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Ward>().Where(x => x.ProvinceId == request.ProvinceId).Select(x => new GetFilteredWardResponseItemModel { Id = x.Id, WardCode = x.WardCode, Name = x.Name, ProvinceId = x.ProvinceId });
        return query.ToFilterResultAsync(request, cancellationToken);
    }
}

using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;
using RRMS.Infrastructure.Persistence.Extensions;

namespace RRMS.Infrastructure.Repositories;

public class ProvinceRepository : GenericRepository<Province>, IProvinceRepository
{
    public ProvinceRepository(Serilog.ILogger logger, AppDbContext context) : base(logger, context) { }

    public Task<FilterResult<GetFilteredProvinceResponseItemModel>> FindFilteredProvincesAsync(GetFilteredProvincesRequestModel request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Province>().Select(x => new GetFilteredProvinceResponseItemModel { Id = x.Id, ProvinceCode = x.ProvinceCode, Name = x.Name, ShortName = x.ShortName, Code = x.Code, PlaceType = x.PlaceType, Country = x.Country });
        return query.ToFilterResultAsync(request, cancellationToken);
    }
}

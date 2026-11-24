using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;
using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IProvinceRepository : IGenericRepository<Province>
{
    Task<FilterResult<GetFilteredProvinceResponseItemModel>> FindFilteredProvincesAsync(GetFilteredProvincesRequestModel request, CancellationToken cancellationToken = default);
}

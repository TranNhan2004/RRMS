using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;
using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Repositories;

public interface IWardRepository : IGenericRepository<Ward>
{
    Task<FilterResult<GetFilteredWardResponseItemModel>> FindFilteredWardsAsync(
        GetFilteredWardsRequestModel request,
        CancellationToken cancellationToken = default);
}

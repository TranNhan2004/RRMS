using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;

namespace RRMS.Application.Interfaces.Services.Addresses;

public interface IAddressQueryService
{
    Task<FilterResult<GetFilteredProvinceResponseItemModel>> GetFilteredProvincesAsync(GetFilteredProvincesRequestModel request, CancellationToken cancellationToken = default);
    Task<FilterResult<GetFilteredWardResponseItemModel>> GetFilteredWardsAsync(GetFilteredWardsRequestModel request, CancellationToken cancellationToken = default);
}

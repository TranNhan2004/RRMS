using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Addresses;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;

namespace RRMS.Application.Services.Addresses;

public class AddressQueryService(IProvinceRepository provinces, IWardRepository wards) : IAddressQueryService
{
    public Task<FilterResult<GetFilteredProvinceResponseItemModel>> GetFilteredProvincesAsync(GetFilteredProvincesRequestModel request, CancellationToken cancellationToken = default) => provinces.FindFilteredProvincesAsync(request, cancellationToken);
    public Task<FilterResult<GetFilteredWardResponseItemModel>> GetFilteredWardsAsync(GetFilteredWardsRequestModel request, CancellationToken cancellationToken = default) => wards.FindFilteredWardsAsync(request, cancellationToken);
}

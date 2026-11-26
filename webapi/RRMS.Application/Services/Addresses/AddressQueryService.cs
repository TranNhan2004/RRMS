using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Addresses;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;

namespace RRMS.Application.Services.Addresses;

public class AddressQueryService : IAddressQueryService
{
    private readonly IProvinceRepository _provinceRepository;
    private readonly IWardRepository _wardRepository;

    public AddressQueryService(IProvinceRepository provinceRepository, IWardRepository wardRepository)
    {
        _provinceRepository = provinceRepository;
        _wardRepository = wardRepository;
    }

    public Task<FilterResult<GetFilteredProvinceResponseItemModel>> GetFilteredProvincesAsync(
        GetFilteredProvincesRequestModel request,
        CancellationToken cancellationToken = default)
    {
        return _provinceRepository.FindFilteredProvincesAsync(request, cancellationToken);
    }

    public Task<FilterResult<GetFilteredWardResponseItemModel>> GetFilteredWardsAsync(
        GetFilteredWardsRequestModel request,
        CancellationToken cancellationToken = default)
    {
        return _wardRepository.FindFilteredWardsAsync(request, cancellationToken);
    }
}

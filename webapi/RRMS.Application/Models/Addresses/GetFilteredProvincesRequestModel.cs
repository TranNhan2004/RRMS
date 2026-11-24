using RRMS.Application.Models.Common;

namespace RRMS.Application.Models.Addresses;

public class GetFilteredProvincesRequestModel : FilterQuery<GetFilteredProvinceResponseItemModel>
{

    public override FilterQueryMap<GetFilteredProvinceResponseItemModel> GetFilterQueryMap() =>
        new FilterQueryMap<GetFilteredProvinceResponseItemModel>().Map("name", x => x.Name);
}

public class GetFilteredProvinceResponseItemModel
{
    public Guid Id { get; init; }
    public string ProvinceCode { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ShortName { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string PlaceType { get; init; } = null!;
    public string Country { get; init; } = null!;
}

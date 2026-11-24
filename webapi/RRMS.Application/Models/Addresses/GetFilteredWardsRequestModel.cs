using RRMS.Application.Models.Common;

namespace RRMS.Application.Models.Addresses;

public class GetFilteredWardsRequestModel : FilterQuery<GetFilteredWardResponseItemModel>
{
    public Guid ProvinceId { get; init; }
    public override FilterQueryMap<GetFilteredWardResponseItemModel> GetFilterQueryMap() =>
        new FilterQueryMap<GetFilteredWardResponseItemModel>().Map("name", x => x.Name);
}

public class GetFilteredWardResponseItemModel
{
    public Guid Id { get; init; }
    public string WardCode { get; init; } = null!;
    public string Name { get; init; } = null!;
    public Guid ProvinceId { get; init; }
}

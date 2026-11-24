using RRMS.Api.Dtos.Common;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;

namespace RRMS.Api.Dtos.V1.Addresses;

public class GetFilteredWardsResponse : FilterResponse<GetFilteredWardResponseItem>
{
    public static GetFilteredWardsResponse FromModel(FilterResult<GetFilteredWardResponseItemModel> result) => new()
    {
        Items = [.. result.Items.Select(item => new GetFilteredWardResponseItem { Id = item.Id, WardCode = item.WardCode, Name = item.Name, ProvinceId = item.ProvinceId })],
        CurrentPage = result.CurrentPage, ItemsPerPage = result.ItemsPerPage,
        TotalItems = result.TotalItems, TotalPages = result.TotalPages
    };
}

public class GetFilteredWardResponseItem
{
    public Guid Id { get; init; }
    public string WardCode { get; init; } = null!;
    public string Name { get; init; } = null!;
    public Guid ProvinceId { get; init; }
}

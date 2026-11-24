using RRMS.Api.Dtos.Common;
using RRMS.Application.Models.Addresses;
using RRMS.Application.Models.Common;

namespace RRMS.Api.Dtos.V1.Addresses;

public class GetFilteredProvincesResponse : FilterResponse<GetFilteredProvinceResponseItem>
{
    public static GetFilteredProvincesResponse FromModel(FilterResult<GetFilteredProvinceResponseItemModel> result) => new()
    {
        Items = [.. result.Items.Select(item => new GetFilteredProvinceResponseItem { Id = item.Id, ProvinceCode = item.ProvinceCode, Name = item.Name, ShortName = item.ShortName, Code = item.Code, PlaceType = item.PlaceType, Country = item.Country })],
        CurrentPage = result.CurrentPage, ItemsPerPage = result.ItemsPerPage,
        TotalItems = result.TotalItems, TotalPages = result.TotalPages
    };
}

public class GetFilteredProvinceResponseItem
{
    public Guid Id { get; init; }
    public string ProvinceCode { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ShortName { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string PlaceType { get; init; } = null!;
    public string Country { get; init; } = null!;
}

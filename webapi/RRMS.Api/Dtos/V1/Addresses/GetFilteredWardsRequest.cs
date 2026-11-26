using RRMS.Api.Dtos.Common;
using RRMS.Application.Models.Addresses;
using System.ComponentModel.DataAnnotations;

namespace RRMS.Api.Dtos.V1.Addresses;

public class GetFilteredWardsRequest : FilterRequest<GetFilteredWardResponseItem>
{
    [Required]
    public Guid? ProvinceId { get; init; }
    public override FilterQueryMapRequest<GetFilteredWardResponseItem> GetFilterRequestMap() => new FilterQueryMapRequest<GetFilteredWardResponseItem>().Map("name", x => x.Name);

    public static GetFilteredWardsRequestModel ToModel(GetFilteredWardsRequest request) => new()
    {
        CurrentPage = request.CurrentPage,
        ItemsPerPage = request.ItemsPerPage,
        SearchGlobalText = request.SearchGlobalText,
        FilterGroup = request.BuildFilterGroup(),
        FilterCriteria = request.BuildFilterCriteria(),
        SortFields = request.BuildSortFields(),
        TimeZoneId = request.ParseTimeZone(),
        ProvinceId = request.ProvinceId ?? throw new ArgumentException("ProvinceId is required.")
    };
}

using RRMS.Api.Dtos.Common;
using RRMS.Application.Models.Addresses;


namespace RRMS.Api.Dtos.V1.Addresses;

public class GetFilteredProvincesRequest : FilterRequest<GetFilteredProvinceResponseItem>
{

    public override FilterQueryMapRequest<GetFilteredProvinceResponseItem> GetFilterRequestMap() => new FilterQueryMapRequest<GetFilteredProvinceResponseItem>().Map("name", x => x.Name);

    public static GetFilteredProvincesRequestModel ToModel(GetFilteredProvincesRequest request) => new()
    {
        CurrentPage = request.CurrentPage,
        ItemsPerPage = request.ItemsPerPage,
        SearchGlobalText = request.SearchGlobalText,
        FilterGroup = request.BuildFilterGroup(),
        FilterCriteria = request.BuildFilterCriteria(),
        SortFields = request.BuildSortFields(),
        TimeZoneId = request.ParseTimeZone()

    };
}

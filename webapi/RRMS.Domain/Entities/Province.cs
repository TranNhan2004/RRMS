using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class Province : BaseEntity
{
    public string ProvinceCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string PlaceType { get; set; } = null!;
    public string Country { get; set; } = null!;
}

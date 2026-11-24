using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class Ward : BaseEntity
{
    public string WardCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Guid ProvinceId { get; set; }
    public Province Province { get; set; } = null!;
}

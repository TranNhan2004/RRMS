using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class Role : BaseEntity
{
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
}

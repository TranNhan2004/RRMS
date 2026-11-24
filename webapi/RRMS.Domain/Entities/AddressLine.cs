using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class AddressLine : AuditableEntity
{
    public string Details { get; set; } = null!;
    public Guid WardId { get; set; }
    public Ward Ward { get; set; } = null!;
}

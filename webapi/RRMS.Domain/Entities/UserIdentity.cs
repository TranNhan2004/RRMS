using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class UserIdentity : AuditableEntity
{
    public UserIdentityProvider Provider { get; set; }
    public string ProviderSubject { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

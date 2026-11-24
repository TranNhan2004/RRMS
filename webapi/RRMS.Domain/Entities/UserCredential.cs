using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class UserCredential : AuditableEntity
{
    public string PasswordHash { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class User : AuditableEntity
{
    public string? UserName { get; set; }
    public string Email { get; set; } = null!;
    public UserStatus Status { get; set; }
    public bool IsEmailVerified { get; set; }
}

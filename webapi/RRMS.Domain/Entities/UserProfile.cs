using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class UserProfile : AuditableEntity
{
    public string? NationalIdNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateTimeOffset? AddressUpdatedAt { get; set; }
    public UserSex? Sex { get; set; }
    public Guid? AddressLineId { get; set; }
    public AddressLine? AddressLine { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

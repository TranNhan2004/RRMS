namespace RRMS.Domain.Entities;

public class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public Guid? UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Domain.Entities;

public class AuditLog : BaseEntity
{
    public AuditAction Action { get; set; }
    public string? TraceId { get; set; }
    public string? RequestId { get; set; }
    public Guid? TransactionId { get; set; }
    public string? ClientActionId { get; set; }
    public string? ClientRequestId { get; set; }
    public string? IpAddress { get; set; }
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string ActorId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

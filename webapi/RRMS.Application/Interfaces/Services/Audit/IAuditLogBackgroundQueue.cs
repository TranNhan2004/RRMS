using RRMS.Domain.Entities;

namespace RRMS.Application.Interfaces.Services.Audit;

public interface IAuditLogBackgroundQueue
{
    void Enqueue(IReadOnlyCollection<AuditLog> auditLogs);
    IAsyncEnumerable<IReadOnlyCollection<AuditLog>> ReadAllAsync(CancellationToken cancellationToken);
}
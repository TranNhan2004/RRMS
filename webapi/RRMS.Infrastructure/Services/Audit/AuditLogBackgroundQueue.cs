using System.Threading.Channels;
using RRMS.Application.Interfaces.Services.Audit;
using RRMS.Domain.Entities;

namespace RRMS.Infrastructure.Services.Audit;

public sealed class AuditLogBackgroundQueue : IAuditLogBackgroundQueue
{
    private readonly Channel<IReadOnlyCollection<AuditLog>> _channel =
        Channel.CreateUnbounded<IReadOnlyCollection<AuditLog>>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

    public void Enqueue(IReadOnlyCollection<AuditLog> auditLogs)
    {
        if (auditLogs.Count > 0)
        {
            _channel.Writer.TryWrite(auditLogs);
        }
    }

    public IAsyncEnumerable<IReadOnlyCollection<AuditLog>> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

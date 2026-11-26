using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TraceId)
            .HasColumnName("trace_id")
            .HasMaxLength(64);

        builder.Property(x => x.RequestId)
            .HasColumnName("request_id")
            .HasMaxLength(255);

        builder.Property(x => x.TransactionId)
            .HasColumnName("transaction_id")
            .HasColumnType("char(36)");

        builder.Property(x => x.ClientActionId)
            .HasColumnName("client_action_id")
            .HasMaxLength(255);

        builder.Property(x => x.ClientRequestId)
            .HasColumnName("client_request_id")
            .HasMaxLength(255);

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.Property(x => x.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(255);

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(255);

        builder.Property(x => x.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("json");

        builder.Property(x => x.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("json");

        builder.Property(x => x.ActorId)
            .HasColumnName("actor_id")
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}

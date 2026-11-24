using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class UserCredentialConfiguration : IEntityTypeConfiguration<UserCredential>
{
    public void Configure(EntityTypeBuilder<UserCredential> builder)
    {
        builder.ToTable("user_credentials");

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255);

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType("char(36)");

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<UserCredential>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PasswordHash)
            .HasAnnotation("RRMS:AuditIgnore", true);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Uam;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("user_identities");

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<UserIdentityProvider>(value, true))
            .HasMaxLength(50);

        builder.Property(x => x.ProviderSubject)
            .HasColumnName("provider_subject")
            .HasMaxLength(255)
            .UseCollation("utf8mb4_bin");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType("char(36)");

        builder.HasIndex(x => new { x.Provider, x.ProviderSubject })
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<UserIdentity>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

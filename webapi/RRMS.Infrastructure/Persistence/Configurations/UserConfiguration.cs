using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Users;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(x => x.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(64);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<UserStatus>(value, true))
            .HasMaxLength(50);

        builder.Property(x => x.IsEmailVerified)
            .HasColumnName("is_email_verified");

        builder.HasIndex(x => x.UserName)
            .IsUnique();

        builder.HasIndex(x => x.Email)
            .IsUnique();
    }
}

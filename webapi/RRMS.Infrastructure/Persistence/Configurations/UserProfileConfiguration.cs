using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.Property(x => x.NationalIdNumber)
            .HasColumnName("national_id_number")
            .HasMaxLength(20);

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(255);

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(255);

        builder.Property(x => x.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20);

        builder.Property(x => x.BirthDate)
            .HasColumnName("birth_date");

        builder.Property(x => x.AddressUpdatedAt)
            .HasColumnName("address_updated_at");

        builder.Property(x => x.Sex)
            .HasColumnName("sex")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.AddressLineId)
            .HasColumnName("address_line_id")
            .HasColumnType("char(36)");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType("char(36)");

        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<UserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AddressLine)
            .WithMany()
            .HasForeignKey(x => x.AddressLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

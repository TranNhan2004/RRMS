using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards");

        builder.Property(x => x.WardCode)
            .HasColumnName("ward_code")
            .HasMaxLength(10);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255);

        builder.Property(x => x.ProvinceId)
            .HasColumnName("province_id")
            .HasColumnType("char(36)");

        builder.HasOne(x => x.Province)
            .WithMany()
            .HasForeignKey(x => x.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("provinces");

        builder.Property(x => x.ProvinceCode)
            .HasColumnName("province_code")
            .HasMaxLength(10);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255);

        builder.Property(x => x.ShortName)
            .HasColumnName("short_name")
            .HasMaxLength(255);

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(10);

        builder.Property(x => x.PlaceType)
            .HasColumnName("place_type")
            .HasMaxLength(255);

        builder.Property(x => x.Country)
            .HasColumnName("country")
            .HasMaxLength(10);

        builder.HasIndex(x => x.Name)
            .IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RRMS.Domain.Entities;

namespace RRMS.Infrastructure.Persistence.Configurations;

public class AddressLineConfiguration : IEntityTypeConfiguration<AddressLine>
{
    public void Configure(EntityTypeBuilder<AddressLine> builder)
    {
        builder.ToTable("address_lines");

        builder.Property(x => x.Details)
            .HasColumnName("details")
            .HasMaxLength(1024);

        builder.Property(x => x.WardId)
            .HasColumnName("ward_id")
            .HasColumnType("char(36)");

        builder.HasOne(x => x.Ward)
            .WithMany()
            .HasForeignKey(x => x.WardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

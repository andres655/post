using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.TaxId)
            .HasMaxLength(50);

        builder.Property(b => b.Phone)
            .HasMaxLength(30);

        builder.Property(b => b.Address)
            .HasMaxLength(500);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(b => b.TimeZone)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.BusinessType)
            .IsRequired();

        builder.Property(b => b.CreatedBy)
            .HasMaxLength(256);

        builder.Property(b => b.UpdatedBy)
            .HasMaxLength(256);

        builder.HasIndex(b => b.Name);
        builder.HasIndex(b => b.IsActive);
    }
}

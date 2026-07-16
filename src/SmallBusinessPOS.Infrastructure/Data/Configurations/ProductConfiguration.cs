using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Barcode)
            .HasMaxLength(100);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.SalePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.EstimatedCost)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.ProductType)
            .IsRequired();

        builder.Property(p => p.UnitOfMeasure)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(256);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(256);

        builder.HasOne(p => p.Business)
            .WithMany()
            .HasForeignKey(p => p.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Código único por negocio
        builder.HasIndex(p => new { p.BusinessId, p.Code }).IsUnique();
        builder.HasIndex(p => new { p.BusinessId, p.Barcode })
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL");
        builder.HasIndex(p => new { p.BusinessId, p.IsActive });
        builder.HasIndex(p => p.Name);
    }
}

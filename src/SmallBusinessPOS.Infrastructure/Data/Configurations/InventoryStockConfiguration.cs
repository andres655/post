using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class InventoryStockConfiguration : IEntityTypeConfiguration<InventoryStock>
{
    public void Configure(EntityTypeBuilder<InventoryStock> builder)
    {
        builder.ToTable("InventoryStocks");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Quantity)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(s => s.MinimumQuantity)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(256);

        builder.Property(s => s.UpdatedBy)
            .HasMaxLength(256);

        // Token de concurrencia para evitar conflictos de actualización simultánea
        builder.Property(s => s.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(s => s.Business)
            .WithMany()
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una entrada de stock única por producto/sucursal
        builder.HasIndex(s => new { s.BusinessId, s.BranchId, s.ProductId }).IsUnique();
    }
}

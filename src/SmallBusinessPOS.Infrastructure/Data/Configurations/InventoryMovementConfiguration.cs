using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Quantity)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(m => m.PreviousQuantity)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(m => m.NewQuantity)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(m => m.MovementType).IsRequired();

        builder.Property(m => m.ReferenceType)
            .HasMaxLength(100);

        builder.Property(m => m.Reason)
            .HasMaxLength(500);

        builder.Property(m => m.DeviceId)
            .HasMaxLength(100);

        builder.Property(m => m.CreatedBy)
            .HasMaxLength(256);

        builder.Property(m => m.UpdatedBy)
            .HasMaxLength(256);

        builder.HasOne(m => m.Business)
            .WithMany()
            .HasForeignKey(m => m.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Branch)
            .WithMany()
            .HasForeignKey(m => m.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.BusinessId, m.BranchId, m.ProductId });
        builder.HasIndex(m => m.CreatedAtUtc);
        builder.HasIndex(m => new { m.ReferenceType, m.ReferenceId })
            .HasFilter("[ReferenceType] IS NOT NULL");
    }
}

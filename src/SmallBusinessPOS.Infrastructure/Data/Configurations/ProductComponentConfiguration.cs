using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class ProductComponentConfiguration : IEntityTypeConfiguration<ProductComponent>
{
    public void Configure(EntityTypeBuilder<ProductComponent> builder)
    {
        builder.ToTable("ProductComponents");
        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.Id).ValueGeneratedNever();

        builder.Property(pc => pc.Quantity)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.HasOne(pc => pc.ParentProduct)
            .WithMany(p => p.Components)
            .HasForeignKey(pc => pc.ParentProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // No cascade desde ComponentProduct para evitar borrar componentes accidentalmente
        builder.HasOne(pc => pc.ComponentProduct)
            .WithMany()
            .HasForeignKey(pc => pc.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un componente no puede repetirse en el mismo padre
        builder.HasIndex(pc => new { pc.ParentProductId, pc.ComponentProductId }).IsUnique();
    }
}

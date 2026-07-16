using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class ProductionEntryConfiguration : IEntityTypeConfiguration<ProductionEntry>
{
    public void Configure(EntityTypeBuilder<ProductionEntry> builder)
    {
        builder.ToTable("ProductionEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Number)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.ConfirmedBy).HasMaxLength(256);
        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.UpdatedBy).HasMaxLength(256);

        builder.HasMany(e => e.Details)
            .WithOne(d => d.ProductionEntry)
            .HasForeignKey(d => d.ProductionEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Business)
            .WithMany()
            .HasForeignKey(e => e.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.BusinessId, e.Number }).IsUnique();
        builder.HasIndex(e => new { e.BusinessId, e.BranchId, e.ProductionDate });
        builder.HasIndex(e => e.Status);
    }
}

public class ProductionEntryDetailConfiguration : IEntityTypeConfiguration<ProductionEntryDetail>
{
    public void Configure(EntityTypeBuilder<ProductionEntryDetail> builder)
    {
        builder.ToTable("ProductionEntryDetails");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.QuantityProduced)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(d => d.QuantityWasted)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(d => d.UnitCost)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ProductId);
    }
}

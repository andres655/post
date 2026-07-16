using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Address)
            .HasMaxLength(500);

        builder.Property(b => b.Phone)
            .HasMaxLength(30);

        builder.Property(b => b.CreatedBy)
            .HasMaxLength(256);

        builder.Property(b => b.UpdatedBy)
            .HasMaxLength(256);

        builder.HasOne(b => b.Business)
            .WithMany(bus => bus.Branches)
            .HasForeignKey(b => b.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Nombre único por negocio
        builder.HasIndex(b => new { b.BusinessId, b.Name }).IsUnique();
        builder.HasIndex(b => b.IsActive);
    }
}

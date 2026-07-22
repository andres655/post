using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class ProductTypeOptionConfiguration : IEntityTypeConfiguration<ProductTypeOption>
{
    public void Configure(EntityTypeBuilder<ProductTypeOption> builder)
    {
        builder.ToTable("ProductTypeOptions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Value)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.UpdatedBy).HasMaxLength(256);

        builder.HasOne(t => t.Business)
            .WithMany()
            .HasForeignKey(t => t.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.BusinessId, t.Value }).IsUnique();
        builder.HasIndex(t => new { t.BusinessId, t.IsActive });
        builder.HasIndex(t => new { t.BusinessId, t.SortOrder });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.CreatedBy).HasMaxLength(256);
        builder.Property(c => c.UpdatedBy).HasMaxLength(256);

        builder.HasOne(c => c.Business)
            .WithMany()
            .HasForeignKey(c => c.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.BusinessId, c.Name }).IsUnique();
        builder.HasIndex(c => new { c.BusinessId, c.IsActive });
        builder.HasIndex(c => new { c.BusinessId, c.SortOrder });
    }
}

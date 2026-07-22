using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Concept)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.UpdatedBy).HasMaxLength(256);

        builder.HasOne(e => e.Business)
            .WithMany()
            .HasForeignKey(e => e.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Branch)
            .WithMany()
            .HasForeignKey(e => e.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CashSession)
            .WithMany()
            .HasForeignKey(e => e.CashSessionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ExpenseCategory)
            .WithMany()
            .HasForeignKey(e => e.ExpenseCategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.BusinessId, e.BranchId, e.CreatedAtUtc });
        builder.HasIndex(e => e.CashSessionId);
        builder.HasIndex(e => e.ExpenseCategoryId);
        builder.HasIndex(e => e.Category);
    }
}

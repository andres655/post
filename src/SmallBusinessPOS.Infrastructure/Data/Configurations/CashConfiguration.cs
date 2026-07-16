using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegisters");
        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.Id).ValueGeneratedNever();

        builder.Property(cr => cr.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cr => cr.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cr => cr.CreatedBy).HasMaxLength(256);
        builder.Property(cr => cr.UpdatedBy).HasMaxLength(256);

        builder.HasOne(cr => cr.Business)
            .WithMany()
            .HasForeignKey(cr => cr.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cr => cr.Branch)
            .WithMany()
            .HasForeignKey(cr => cr.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cr => new { cr.BranchId, cr.Code }).IsUnique();
        builder.HasIndex(cr => cr.IsActive);
    }
}

public class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("CashSessions");
        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Id).ValueGeneratedNever();

        builder.Property(cs => cs.Status).IsRequired();

        builder.Property(cs => cs.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(cs => cs.TotalIncome).HasColumnType("decimal(18,2)");
        builder.Property(cs => cs.TotalExpenses).HasColumnType("decimal(18,2)");
        builder.Property(cs => cs.ClosingBalance).HasColumnType("decimal(18,2)");
        builder.Property(cs => cs.DeclaredClosingBalance).HasColumnType("decimal(18,2)");
        builder.Property(cs => cs.Difference).HasColumnType("decimal(18,2)");

        builder.Property(cs => cs.Notes).HasMaxLength(1000);

        builder.Property(cs => cs.CreatedBy).HasMaxLength(256);
        builder.Property(cs => cs.UpdatedBy).HasMaxLength(256);

        builder.HasOne(cs => cs.Business)
            .WithMany()
            .HasForeignKey(cs => cs.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cs => cs.Branch)
            .WithMany()
            .HasForeignKey(cs => cs.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cs => cs.CashRegister)
            .WithMany(cr => cr.Sessions)
            .HasForeignKey(cs => cs.CashRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Una sola sesión abierta por caja
        builder.HasIndex(cs => new { cs.CashRegisterId, cs.Status })
            .HasFilter("[Status] = 1"); // 1 = Open
    }
}

public class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements");
        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.Id).ValueGeneratedNever();

        builder.Property(cm => cm.MovementType).IsRequired();

        builder.Property(cm => cm.Amount).HasColumnType("decimal(18,2)");

        builder.Property(cm => cm.Description).HasMaxLength(500);
        builder.Property(cm => cm.ReferenceType).HasMaxLength(100);

        builder.Property(cm => cm.CreatedBy).HasMaxLength(256);
        builder.Property(cm => cm.UpdatedBy).HasMaxLength(256);

        builder.HasOne(cm => cm.Business)
            .WithMany()
            .HasForeignKey(cm => cm.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cm => cm.Branch)
            .WithMany()
            .HasForeignKey(cm => cm.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cm => cm.CashSession)
            .WithMany(cs => cs.Movements)
            .HasForeignKey(cm => cm.CashSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cm => cm.PaymentMethod)
            .WithMany()
            .HasForeignKey(cm => cm.PaymentMethodId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(cm => new { cm.CashSessionId, cm.CreatedAtUtc });
        builder.HasIndex(cm => cm.MovementType);
    }
}

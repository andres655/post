using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.SaleType).IsRequired();
        builder.Property(s => s.Status).IsRequired();

        builder.Property(s => s.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Discount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Tax).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Total).HasColumnType("decimal(18,2)");

        builder.Property(s => s.CustomerName).HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.CancelledBy).HasMaxLength(256);
        builder.Property(s => s.CancellationReason).HasMaxLength(500);

        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.UpdatedBy).HasMaxLength(256);

        builder.HasOne(s => s.Business)
            .WithMany()
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CashSession)
            .WithMany()
            .HasForeignKey(s => s.CashSessionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.ReceiptNumber).IsUnique();
        builder.HasIndex(s => new { s.BranchId, s.SoldAtUtc });
        builder.HasIndex(s => s.Status);
    }
}

public class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
{
    public void Configure(EntityTypeBuilder<SaleDetail> builder)
    {
        builder.ToTable("SaleDetails");
        builder.HasKey(sd => sd.Id);

        builder.Property(sd => sd.Id).ValueGeneratedNever();

        builder.Property(sd => sd.ProductCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sd => sd.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sd => sd.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(sd => sd.UnitPrice).HasColumnType("decimal(18,2)");

        builder.HasOne(sd => sd.Sale)
            .WithMany(s => s.Details)
            .HasForeignKey(sd => sd.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sd => sd.Product)
            .WithMany()
            .HasForeignKey(sd => sd.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sd => sd.SaleId);
    }
}

public class SalePaymentConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> builder)
    {
        builder.ToTable("SalePayments");
        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id).ValueGeneratedNever();

        builder.Property(sp => sp.Amount).HasColumnType("decimal(18,2)");
        builder.Property(sp => sp.TenderedAmount).HasColumnType("decimal(18,2)");
        builder.Property(sp => sp.Reference).HasMaxLength(100);

        builder.HasOne(sp => sp.Sale)
            .WithMany(s => s.Payments)
            .HasForeignKey(sp => sp.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.PaymentMethod)
            .WithMany()
            .HasForeignKey(sp => sp.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sp => sp.SaleId);
    }
}

public class SaleNumberSequenceConfiguration : IEntityTypeConfiguration<SaleNumberSequence>
{
    public void Configure(EntityTypeBuilder<SaleNumberSequence> builder)
    {
        builder.ToTable("SaleNumberSequences");
        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.Id).ValueGeneratedNever();
        builder.Property(ss => ss.LastSequence).IsRequired();

        builder.Property(ss => ss.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasOne(ss => ss.Business)
            .WithMany()
            .HasForeignKey(ss => ss.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ss => ss.Branch)
            .WithMany()
            .HasForeignKey(ss => ss.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ss => ss.CashRegister)
            .WithMany()
            .HasForeignKey(ss => ss.CashRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Secuencia única por caja y fecha
        builder.HasIndex(ss => new { ss.CashRegisterId, ss.BusinessDate }).IsUnique();
    }
}

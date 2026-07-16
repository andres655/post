using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class ReceiptReprintAuditConfiguration : IEntityTypeConfiguration<ReceiptReprintAudit>
{
    public void Configure(EntityTypeBuilder<ReceiptReprintAudit> builder)
    {
        builder.ToTable("ReceiptReprintAudits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SaleNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ReprintedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.ReprintedAtUtc });
        builder.HasIndex(x => new { x.SaleId, x.ReprintedAtUtc });
    }
}

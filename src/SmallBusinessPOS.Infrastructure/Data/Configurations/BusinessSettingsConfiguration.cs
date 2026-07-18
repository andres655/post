using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class BusinessSettingsConfiguration : IEntityTypeConfiguration<BusinessSettings>
{
    public void Configure(EntityTypeBuilder<BusinessSettings> builder)
    {
        builder.ToTable("BusinessSettings");
        builder.HasKey(bs => bs.Id);

        builder.Property(bs => bs.Id).ValueGeneratedNever();

        builder.Property(bs => bs.CurrencySymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(bs => bs.DefaultTaxRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(bs => bs.ReceiptLogoPath)
            .HasMaxLength(500);

        builder.Property(bs => bs.ReceiptHeader)
            .HasMaxLength(500);

        builder.Property(bs => bs.TicketFooter)
            .HasMaxLength(500);

        builder.HasOne(bs => bs.Business)
            .WithOne()
            .HasForeignKey<BusinessSettings>(bs => bs.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(bs => bs.BusinessId).IsUnique();
    }
}

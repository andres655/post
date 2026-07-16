using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");
        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.Id).ValueGeneratedNever();

        builder.Property(pm => pm.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pm => pm.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pm => pm.Type).IsRequired();

        builder.Property(pm => pm.CreatedBy).HasMaxLength(256);
        builder.Property(pm => pm.UpdatedBy).HasMaxLength(256);

        builder.HasOne(pm => pm.Business)
            .WithMany()
            .HasForeignKey(pm => pm.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pm => new { pm.BusinessId, pm.Code }).IsUnique();
        builder.HasIndex(pm => new { pm.BusinessId, pm.IsActive });
    }
}

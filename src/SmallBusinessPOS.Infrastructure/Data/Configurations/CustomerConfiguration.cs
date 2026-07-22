using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id).ValueGeneratedNever();
        builder.Property(customer => customer.Name).IsRequired().HasMaxLength(200);
        builder.Property(customer => customer.DocumentNumber).HasMaxLength(50);
        builder.Property(customer => customer.Phone).HasMaxLength(30);
        builder.Property(customer => customer.Email).HasMaxLength(256);
        builder.Property(customer => customer.CreatedBy).HasMaxLength(256);
        builder.Property(customer => customer.UpdatedBy).HasMaxLength(256);

        builder.HasOne(customer => customer.Business)
            .WithMany()
            .HasForeignKey(customer => customer.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(customer => new { customer.BusinessId, customer.Name });
        builder.HasIndex(customer => new { customer.BusinessId, customer.DocumentNumber })
            .IsUnique()
            .HasFilter("[DocumentNumber] IS NOT NULL");
        builder.HasIndex(customer => customer.IsActive);
    }
}

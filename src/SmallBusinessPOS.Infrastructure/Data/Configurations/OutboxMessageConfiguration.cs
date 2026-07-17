using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(om => om.Id);

        builder.Property(om => om.Id).ValueGeneratedNever();

        builder.Property(om => om.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(om => om.AggregateType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(om => om.AggregateId)
            .IsRequired();

        builder.Property(om => om.Status)
            .HasConversion<int>()
            .HasDefaultValue(SyncStatus.Pending)
            .IsRequired();

        builder.Property(om => om.MaxRetries)
            .IsRequired();

        builder.Property(om => om.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(om => om.LastError).HasColumnType("nvarchar(max)");

        builder.HasIndex(om => new { om.BusinessId, om.ProcessedAtUtc });
        builder.HasIndex(om => new { om.BusinessId, om.Status, om.CreatedAtUtc });
        builder.HasIndex(om => new { om.EventType, om.AggregateId });
        builder.HasIndex(om => om.CreatedAtUtc);
        builder.HasIndex(om => om.OccurredAtUtc);
    }
}

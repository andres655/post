using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Infrastructure.Data.Configurations;

public class SyncQueueItemConfiguration : IEntityTypeConfiguration<SyncQueueItem>
{
    public void Configure(EntityTypeBuilder<SyncQueueItem> builder)
    {
        builder.ToTable("SyncQueueItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Operation)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(SyncStatus.Pending)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.DeviceId)
            .HasMaxLength(100);

        builder.Property(x => x.LastError)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.BusinessId, x.Status, x.Priority, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.Operation });
        builder.HasIndex(x => x.LastAttemptAtUtc);
    }
}

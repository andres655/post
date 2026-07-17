using FluentAssertions;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Tests;

public class OfflineSynchronizationTests
{
    [Fact]
    public void OutboxMessage_Create_ShouldInitializePendingMessage()
    {
        var businessId = Guid.CreateVersion7();
        var saleId = Guid.CreateVersion7();

        var message = OutboxMessage.Create(
            businessId,
            "SaleConfirmed",
            saleId,
            """{"saleId":"test"}""",
            aggregateType: "Sale");

        message.BusinessId.Should().Be(businessId);
        message.EventType.Should().Be("SaleConfirmed");
        message.AggregateType.Should().Be("Sale");
        message.AggregateId.Should().Be(saleId);
        message.Status.Should().Be(SyncStatus.Pending);
        message.IsProcessed.Should().BeFalse();
        message.RetryCount.Should().Be(0);
        message.MaxRetries.Should().Be(3);
        message.CreatedAtUtc.Should().NotBe(default);
        message.OccurredAtUtc.Should().NotBe(default);
    }

    [Fact]
    public void OutboxMessage_RecordError_ShouldStopRetryingWhenMaxRetriesReached()
    {
        var message = OutboxMessage.Create(
            Guid.CreateVersion7(),
            "SaleConfirmed",
            Guid.CreateVersion7(),
            """{"saleId":"test"}""");

        message.RecordError("Network unavailable", maxRetries: 1);

        message.Status.Should().Be(SyncStatus.Failed);
        message.RetryCount.Should().Be(1);
        message.LastError.Should().Be("Network unavailable");
        message.ProcessedAtUtc.Should().NotBeNull();
        message.IsProcessed.Should().BeTrue();
    }

    [Fact]
    public void SyncQueueItem_Create_ShouldInitializePendingItem()
    {
        var businessId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var entityId = Guid.CreateVersion7();

        var item = SyncQueueItem.Create(
            businessId,
            "Sale",
            entityId,
            SyncOperation.Created,
            """{"total":350}""",
            branchId,
            "POS-01",
            priority: 10);

        item.BusinessId.Should().Be(businessId);
        item.BranchId.Should().Be(branchId);
        item.EntityName.Should().Be("Sale");
        item.EntityId.Should().Be(entityId);
        item.Operation.Should().Be(SyncOperation.Created);
        item.Status.Should().Be(SyncStatus.Pending);
        item.Payload.Should().Be("""{"total":350}""");
        item.DeviceId.Should().Be("POS-01");
        item.Priority.Should().Be(10);
        item.CreatedAtUtc.Should().NotBe(default);
    }

    [Fact]
    public void SyncQueueItem_ShouldMoveBetweenFailedModifiedAndSyncedStates()
    {
        var item = SyncQueueItem.Create(
            Guid.CreateVersion7(),
            "InventoryMovement",
            Guid.CreateVersion7(),
            SyncOperation.Created);

        item.RecordFailure("Timeout");
        item.Status.Should().Be(SyncStatus.Failed);
        item.RetryCount.Should().Be(1);
        item.LastError.Should().Be("Timeout");
        item.LastAttemptAtUtc.Should().NotBeNull();

        item.MarkModified("""{"quantity":39}""");
        item.Status.Should().Be(SyncStatus.Modified);
        item.Payload.Should().Be("""{"quantity":39}""");
        item.LastError.Should().BeNull();

        item.MarkSynced();
        item.Status.Should().Be(SyncStatus.Synced);
        item.SyncedAtUtc.Should().NotBeNull();
        item.LastError.Should().BeNull();
    }
}

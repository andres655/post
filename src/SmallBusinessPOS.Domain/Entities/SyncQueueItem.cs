using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Cola local de cambios por entidad para una sincronizacion remota futura.
/// No reemplaza el outbox de eventos; complementa el tracking offline por registro.
/// </summary>
public class SyncQueueItem : Entity
{
    public Guid BusinessId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string EntityName { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public SyncOperation Operation { get; private set; }
    public string? Payload { get; private set; }
    public SyncStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public int Priority { get; private set; }
    public string? DeviceId { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public DateTime? SyncedAtUtc { get; private set; }

    private SyncQueueItem() { }

    public static SyncQueueItem Create(
        Guid businessId,
        string entityName,
        Guid entityId,
        SyncOperation operation,
        string? payload = null,
        Guid? branchId = null,
        string? deviceId = null,
        int priority = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return new SyncQueueItem
        {
            BusinessId = businessId,
            BranchId = branchId,
            EntityName = entityName.Trim(),
            EntityId = entityId,
            Operation = operation,
            Payload = string.IsNullOrWhiteSpace(payload) ? null : payload,
            Status = SyncStatus.Pending,
            RetryCount = 0,
            Priority = priority,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkModified(string? payload = null)
    {
        Status = SyncStatus.Modified;
        Payload = string.IsNullOrWhiteSpace(payload) ? Payload : payload;
        SyncedAtUtc = null;
        LastError = null;
    }

    public void MarkSynced()
    {
        Status = SyncStatus.Synced;
        SyncedAtUtc = DateTime.UtcNow;
        LastAttemptAtUtc = SyncedAtUtc;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        RetryCount++;
        Status = SyncStatus.Failed;
        LastAttemptAtUtc = DateTime.UtcNow;
        LastError = error.Trim();
    }

    public void ResetPending()
    {
        Status = SyncStatus.Pending;
        LastError = null;
    }
}

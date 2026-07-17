using System.ComponentModel.DataAnnotations.Schema;
using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Outbox pattern — almacena eventos que deben ser sincronizados con el servidor remoto.
/// Se procesa en background para garantizar entrega even si falla la conexión.
/// </summary>
public class OutboxMessage : Entity
{
    public Guid BusinessId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public string Payload { get; private set; } = string.Empty; // JSON
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public SyncStatus Status { get; private set; }
    [NotMapped]
    public bool IsProcessed => ProcessedAtUtc.HasValue || Status == SyncStatus.Synced;
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(
        Guid businessId,
        string eventType,
        Guid aggregateId,
        string payload,
        string? aggregateType = null,
        DateTime? occurredAtUtc = null,
        int maxRetries = 3)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var now = DateTime.UtcNow;

        return new OutboxMessage
        {
            BusinessId = businessId,
            EventType = eventType.Trim(),
            AggregateType = string.IsNullOrWhiteSpace(aggregateType) ? eventType.Trim() : aggregateType.Trim(),
            AggregateId = aggregateId,
            Payload = payload,
            CreatedAtUtc = now,
            OccurredAtUtc = occurredAtUtc ?? now,
            Status = SyncStatus.Pending,
            RetryCount = 0,
            MaxRetries = Math.Max(1, maxRetries)
        };
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        Status = SyncStatus.Synced;
        LastError = null;
    }

    public void RecordError(string error, int maxRetries = 3)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        RetryCount++;
        LastError = error.Trim();
        Status = SyncStatus.Failed;
        MaxRetries = Math.Max(1, maxRetries);

        if (RetryCount >= MaxRetries)
            ProcessedAtUtc = DateTime.UtcNow; // Stop retrying after max attempts.
    }

    public void ResetPending()
    {
        Status = SyncStatus.Pending;
        LastError = null;
        ProcessedAtUtc = null;
    }
}

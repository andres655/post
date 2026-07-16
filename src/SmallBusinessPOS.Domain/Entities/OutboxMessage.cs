using System.ComponentModel.DataAnnotations.Schema;
using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Outbox pattern — almacena eventos que deben ser sincronizados con el servidor remoto.
/// Se procesa en background para garantizar entrega even si falla la conexión.
/// </summary>
public class OutboxMessage : Entity
{
    public Guid BusinessId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public string Payload { get; private set; } = string.Empty; // JSON
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    [NotMapped]
    public bool IsProcessed => ProcessedAtUtc.HasValue;
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(
        Guid businessId,
        string eventType,
        Guid aggregateId,
        string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            BusinessId = businessId,
            EventType = eventType.Trim(),
            AggregateId = aggregateId,
            Payload = payload,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    public void MarkProcessed() => ProcessedAtUtc = DateTime.UtcNow;

    public void RecordError(string error, int maxRetries = 3)
    {
        RetryCount++;
        LastError = error;

        if (RetryCount >= maxRetries)
            MarkProcessed(); // Stop retrying after max attempts
    }
}

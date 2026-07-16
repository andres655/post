namespace SmallBusinessPOS.Domain.Common;

/// <summary>
/// Entidad base que incluye campos de auditoría.
/// Todos los DateTime se almacenan en UTC.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    protected AuditableEntity() : base()
    {
        CreatedAtUtc = DateTime.UtcNow;
    }

    protected AuditableEntity(Guid id) : base(id)
    {
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }

    public void SetUpdated(string updatedBy)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    // Usado internamente para hidratar desde persistencia
    protected void SetAuditFields(DateTime createdAt, string? createdBy, DateTime? updatedAt, string? updatedBy)
    {
        CreatedAtUtc = createdAt;
        CreatedBy = createdBy;
        UpdatedAtUtc = updatedAt;
        UpdatedBy = updatedBy;
    }
}

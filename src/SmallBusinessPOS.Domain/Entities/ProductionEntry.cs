using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

public class ProductionEntry : AuditableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public ProductionEntryStatus Status { get; private set; }
    public DateOnly ProductionDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public string? ConfirmedBy { get; private set; }

    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;

    private readonly List<ProductionEntryDetail> _details = new();
    public IReadOnlyCollection<ProductionEntryDetail> Details => _details.AsReadOnly();

    private ProductionEntry() { }

    public static ProductionEntry Create(
        Guid businessId,
        Guid branchId,
        string number,
        DateOnly productionDate,
        string? notes = null,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        var entry = new ProductionEntry
        {
            BusinessId = businessId,
            BranchId = branchId,
            Number = number.Trim().ToUpperInvariant(),
            Status = ProductionEntryStatus.Draft,
            ProductionDate = productionDate,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        if (createdBy is not null)
            entry.SetCreatedBy(createdBy);

        return entry;
    }

    public void AddDetail(ProductionEntryDetail detail)
    {
        if (Status != ProductionEntryStatus.Draft)
            throw new InvalidOperationException("No se pueden agregar detalles a una produccion confirmada.");

        _details.Add(detail);
    }

    public void Confirm(string? confirmedBy = null)
    {
        if (Status == ProductionEntryStatus.Confirmed)
            throw new InvalidOperationException("La produccion ya esta confirmada.");

        if (!_details.Any())
            throw new InvalidOperationException("La produccion debe tener al menos un detalle.");

        Status = ProductionEntryStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
        ConfirmedBy = confirmedBy;

        if (confirmedBy is not null)
            SetUpdated(confirmedBy);
    }
}

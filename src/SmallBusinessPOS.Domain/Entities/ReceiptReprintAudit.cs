using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Bitácora de reimpresiones de ticket para trazabilidad operativa.
/// </summary>
public class ReceiptReprintAudit : Entity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid SaleId { get; private set; }
    public string SaleNumber { get; private set; } = string.Empty;
    public DateTime ReprintedAtUtc { get; private set; }
    public string ReprintedBy { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;

    private ReceiptReprintAudit() { }

    public static ReceiptReprintAudit Create(
        Guid businessId,
        Guid branchId,
        Guid saleId,
        string saleNumber,
        string reprintedBy,
        string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saleNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(reprintedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new ReceiptReprintAudit
        {
            BusinessId = businessId,
            BranchId = branchId,
            SaleId = saleId,
            SaleNumber = saleNumber.Trim().ToUpperInvariant(),
            ReprintedAtUtc = DateTime.UtcNow,
            ReprintedBy = reprintedBy.Trim(),
            Source = source.Trim()
        };
    }
}

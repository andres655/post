using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

public class SaleReturn : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid? CashSessionId { get; private set; }
    public string ReturnNumber { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? RefundReference { get; private set; }
    public DateTime ReturnedAtUtc { get; private set; }

    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public Sale Sale { get; private set; } = null!;
    public CashSession? CashSession { get; private set; }

    private readonly List<SaleReturnDetail> _details = new();
    public IReadOnlyCollection<SaleReturnDetail> Details => _details.AsReadOnly();

    private SaleReturn() { }

    public static SaleReturn Create(
        Guid businessId,
        Guid branchId,
        Guid saleId,
        string returnNumber,
        string reason,
        Guid? cashSessionId = null,
        string? refundReference = null,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(returnNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var saleReturn = new SaleReturn
        {
            BusinessId = businessId,
            BranchId = branchId,
            SaleId = saleId,
            CashSessionId = cashSessionId,
            ReturnNumber = returnNumber.Trim(),
            Reason = reason.Trim(),
            RefundReference = string.IsNullOrWhiteSpace(refundReference) ? null : refundReference.Trim(),
            ReturnedAtUtc = DateTime.UtcNow
        };

        if (createdBy is not null)
            saleReturn.SetCreatedBy(createdBy);

        return saleReturn;
    }

    public void AddDetail(SaleReturnDetail detail)
    {
        _details.Add(detail);
        Total = _details.Sum(item => item.LineTotal);
    }
}

public class SaleReturnDetail : Entity
{
    public Guid SaleReturnId { get; private set; }
    public Guid SaleDetailId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public SaleReturn SaleReturn { get; private set; } = null!;
    public SaleDetail SaleDetail { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private SaleReturnDetail() { }

    public static SaleReturnDetail Create(
        Guid saleReturnId,
        Guid saleDetailId,
        Guid productId,
        string productCode,
        string productName,
        decimal quantity,
        decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor a cero.");

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");

        return new SaleReturnDetail
        {
            SaleReturnId = saleReturnId,
            SaleDetailId = saleDetailId,
            ProductId = productId,
            ProductCode = productCode.Trim().ToUpperInvariant(),
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}

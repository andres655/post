using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Venta principal.
/// Contiene detalles de productos vendidos.
/// Referencia a sesión de caja y pagos.
/// </summary>
public class Sale : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid? CashSessionId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty; // PRIN-C01-20260716-000001
    public SaleType SaleType { get; private set; }
    public SaleStatus Status { get; private set; }
    public DateTime SoldAtUtc { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Total { get; private set; }
    public string? CustomerName { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navegación
    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public CashSession? CashSession { get; private set; }

    private readonly List<SaleDetail> _details = new();
    public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

    private readonly List<SalePayment> _payments = new();
    public IReadOnlyCollection<SalePayment> Payments => _payments.AsReadOnly();

    private Sale() { }

    public static Sale Create(
        Guid businessId,
        Guid branchId,
        string receiptNumber,
        SaleType saleType,
        Guid? cashSessionId = null,
        string? customerName = null,
        string? notes = null,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);

        var sale = new Sale
        {
            BusinessId = businessId,
            BranchId = branchId,
            CashSessionId = cashSessionId,
            ReceiptNumber = receiptNumber.Trim(),
            SaleType = saleType,
            Status = SaleStatus.Draft,
            SoldAtUtc = DateTime.UtcNow,
            CustomerName = customerName?.Trim(),
            Notes = notes?.Trim()
        };

        if (createdBy is not null)
            sale.SetCreatedBy(createdBy);

        return sale;
    }

    /// <summary>
    /// Agrega un detalle a la venta (producto).
    /// </summary>
    public void AddDetail(SaleDetail detail)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("No se pueden agregar detalles a una venta confirmada.");

        _details.Add(detail);
        RecalculateTotals();
    }

    /// <summary>
    /// Registra un pago para la venta.
    /// </summary>
    public void AddPayment(SalePayment payment)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("No se pueden agregar pagos a una venta confirmada.");

        _payments.Add(payment);
    }

    public void ApplyFinancials(decimal discount, decimal tax)
    {
        if (discount < 0)
            throw new ArgumentOutOfRangeException(nameof(discount), "El descuento no puede ser negativo.");

        if (tax < 0)
            throw new ArgumentOutOfRangeException(nameof(tax), "El impuesto no puede ser negativo.");

        Discount = discount;
        Tax = tax;
        Total = SubTotal - Discount + Tax;
    }

    /// <summary>
    /// Confirma la venta — se vuelve inmutable, se registra en caja.
    /// </summary>
    public void Confirm(string? confirmedBy = null)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("La venta ya está confirmada o cancelada.");

        if (!_details.Any())
            throw new InvalidOperationException("No hay detalles para confirmar la venta.");

        var paymentTotal = _payments.Sum(p => p.Amount);
        if (paymentTotal != Total)
            throw new InvalidOperationException("La suma de pagos no coincide con el total de la venta.");

        Status = SaleStatus.Confirmed;

        if (confirmedBy is not null)
            SetUpdated(confirmedBy);
    }

    /// <summary>
    /// Cancela la venta.
    /// </summary>
    public void Cancel(string reason, string? cancelledBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("La venta ya está cancelada.");

        if (Status != SaleStatus.Confirmed)
            throw new InvalidOperationException("Solo se pueden cancelar ventas confirmadas.");

        Status = SaleStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancelledBy = cancelledBy;
        CancellationReason = reason.Trim();

        if (cancelledBy is not null)
            SetUpdated(cancelledBy);
    }

    private void RecalculateTotals()
    {
        SubTotal = _details.Sum(d => d.UnitPrice * d.Quantity);
        // Discount y Tax se asignan manualmente antes de confirmar
        Total = SubTotal - Discount + Tax;
    }
}

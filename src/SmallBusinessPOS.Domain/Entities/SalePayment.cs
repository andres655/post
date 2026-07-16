using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Pago de una venta — puede haber múltiples pagos (ej: efectivo + tarjeta).
/// </summary>
public class SalePayment : Entity
{
    public Guid SaleId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; } // Número de tarjeta, cheque, etc.
    public DateTime CreatedAtUtc { get; private set; }

    // Navegación
    public Sale Sale { get; private set; } = null!;
    public PaymentMethod PaymentMethod { get; private set; } = null!;

    private SalePayment() { }

    public static SalePayment Create(
        Guid saleId,
        Guid paymentMethodId,
        decimal amount,
        string? reference = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto debe ser mayor a cero.");

        return new SalePayment
        {
            SaleId = saleId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            Reference = reference?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

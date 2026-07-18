using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Pago de una venta. Amount es el monto aplicado; TenderedAmount es lo recibido del cliente.
/// </summary>
public class SalePayment : Entity
{
    public Guid SaleId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal TenderedAmount { get; private set; }
    public string? Reference { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public Sale Sale { get; private set; } = null!;
    public PaymentMethod PaymentMethod { get; private set; } = null!;

    private SalePayment() { }

    public static SalePayment Create(
        Guid saleId,
        Guid paymentMethodId,
        decimal amount,
        decimal? tenderedAmount = null,
        string? reference = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto debe ser mayor a cero.");

        var received = tenderedAmount ?? amount;
        if (received < amount)
            throw new ArgumentOutOfRangeException(nameof(tenderedAmount), "El monto recibido no puede ser menor al monto aplicado.");

        return new SalePayment
        {
            SaleId = saleId,
            PaymentMethodId = paymentMethodId,
            Amount = amount,
            TenderedAmount = received,
            Reference = reference?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

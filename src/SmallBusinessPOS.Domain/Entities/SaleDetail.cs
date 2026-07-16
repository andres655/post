using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Detalle de venta — cada línea de producto vendido.
/// Snapshots del producto al momento de la venta.
/// </summary>
public class SaleDetail : Entity
{
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    // Navegación
    public Sale Sale { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private SaleDetail() { }

    public static SaleDetail Create(
        Guid saleId,
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

        return new SaleDetail
        {
            SaleId = saleId,
            ProductId = productId,
            ProductCode = productCode.Trim().ToUpperInvariant(),
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}

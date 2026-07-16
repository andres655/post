using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Componente de un producto compuesto o combo.
/// Permite modelar:
///   - Combo familiar = 1 Pollo + 1 Yuca + 1 Ensalada + 1 Refresco
///   - Medio pollo = 0.5 Pollo entero
///   - Cuarto de pollo = 0.25 Pollo entero
/// </summary>
public class ProductComponent : Entity
{
    public Guid ParentProductId { get; private set; }
    public Guid ComponentProductId { get; private set; }

    /// <summary>
    /// Cantidad del componente que se consume al vender/usar el producto padre.
    /// Admite fraccionario (decimal(18,4)) para medios y cuartos.
    /// </summary>
    public decimal Quantity { get; private set; }

    // Navegación EF Core
    public Product ParentProduct { get; private set; } = null!;
    public Product ComponentProduct { get; private set; } = null!;

    private ProductComponent() { }

    public static ProductComponent Create(Guid parentProductId, Guid componentProductId, decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad del componente debe ser mayor a cero.");

        if (parentProductId == componentProductId)
            throw new ArgumentException("Un producto no puede ser componente de sí mismo.");

        return new ProductComponent
        {
            ParentProductId = parentProductId,
            ComponentProductId = componentProductId,
            Quantity = quantity
        };
    }

    public void UpdateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad del componente debe ser mayor a cero.");

        Quantity = quantity;
    }
}

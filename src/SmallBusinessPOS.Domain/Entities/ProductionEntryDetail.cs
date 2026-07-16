using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

public class ProductionEntryDetail : Entity
{
    public Guid ProductionEntryId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal QuantityProduced { get; private set; }
    public decimal QuantityWasted { get; private set; }
    public decimal UnitCost { get; private set; }

    public ProductionEntry ProductionEntry { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private ProductionEntryDetail() { }

    public static ProductionEntryDetail Create(
        Guid productionEntryId,
        Guid productId,
        decimal quantityProduced,
        decimal unitCost = 0m,
        decimal quantityWasted = 0m)
    {
        if (quantityProduced <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityProduced), "La cantidad producida debe ser mayor a cero.");

        if (quantityWasted < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityWasted), "La merma no puede ser negativa.");

        if (quantityWasted > quantityProduced)
            throw new ArgumentOutOfRangeException(nameof(quantityWasted), "La merma no puede ser mayor que la cantidad producida.");

        if (unitCost < 0)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "El costo unitario no puede ser negativo.");

        return new ProductionEntryDetail
        {
            ProductionEntryId = productionEntryId,
            ProductId = productId,
            QuantityProduced = quantityProduced,
            QuantityWasted = quantityWasted,
            UnitCost = unitCost
        };
    }
}

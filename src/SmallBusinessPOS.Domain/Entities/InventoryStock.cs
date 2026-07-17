using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Existencia actual de un producto en una sucursal.
/// Solo se modifica a través de InventoryMovement.
/// Incluye token de concurrencia para evitar conflictos simultáneos.
/// </summary>
public class InventoryStock : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal MinimumQuantity { get; private set; }

    /// <summary>Token de concurrencia — gestionado por EF Core.</summary>
    public byte[]? RowVersion { get; private set; }

    // Navegación EF Core
    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private InventoryStock() { }

    public static InventoryStock Create(Guid businessId, Guid branchId, Guid productId, decimal initialQuantity = 0m)
    {
        return new InventoryStock
        {
            BusinessId = businessId,
            BranchId = branchId,
            ProductId = productId,
            Quantity = initialQuantity,
            MinimumQuantity = 0m
        };
    }

    public void ApplyMovement(decimal newQuantity)
    {
        Quantity = newQuantity;
    }

    public void SetMinimumQuantity(decimal minimum)
    {
        if (minimum < 0)
            throw new ArgumentOutOfRangeException(nameof(minimum), "El mínimo no puede ser negativo.");

        MinimumQuantity = minimum;
    }

    public bool IsBelowMinimum() => Quantity <= MinimumQuantity;
}

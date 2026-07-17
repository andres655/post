using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Registro inmutable de cada movimiento de inventario.
/// Nunca se elimina. Una anulación crea movimientos compensatorios.
/// </summary>
public class InventoryMovement : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public MovementType MovementType { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal PreviousQuantity { get; private set; }
    public decimal NewQuantity { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Reason { get; private set; }
    public string? DeviceId { get; private set; }
    public SyncStatus SyncStatus { get; private set; } = SyncStatus.Pending;

    // Navegación EF Core
    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private InventoryMovement() { }

    public static InventoryMovement Create(
        Guid businessId,
        Guid branchId,
        Guid productId,
        MovementType movementType,
        decimal quantity,
        decimal previousQuantity,
        decimal newQuantity,
        string? referenceType = null,
        Guid? referenceId = null,
        string? reason = null,
        string? deviceId = null,
        string? createdBy = null)
    {
        var movement = new InventoryMovement
        {
            BusinessId = businessId,
            BranchId = branchId,
            ProductId = productId,
            MovementType = movementType,
            Quantity = quantity,
            PreviousQuantity = previousQuantity,
            NewQuantity = newQuantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Reason = reason,
            DeviceId = deviceId,
            SyncStatus = SyncStatus.Pending
        };

        if (createdBy is not null)
            movement.SetCreatedBy(createdBy);

        return movement;
    }

    public void MarkSynced() => SyncStatus = SyncStatus.Synced;
}

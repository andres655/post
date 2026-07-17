using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Movimiento de caja — registro inmutable de cada transacción en la caja.
/// Nunca se elimina, solo se anula con compensación.
/// </summary>
public class CashMovement : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid CashSessionId { get; private set; }
    public CashMovementType MovementType { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public Guid? PaymentMethodId { get; private set; }

    // Navegación
    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public CashSession CashSession { get; private set; } = null!;
    public PaymentMethod? PaymentMethod { get; private set; }

    private CashMovement() { }

    public static CashMovement Create(
        Guid businessId,
        Guid branchId,
        Guid cashSessionId,
        CashMovementType movementType,
        decimal amount,
        string? description = null,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? paymentMethodId = null,
        string? createdBy = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto debe ser mayor a cero.");

        var movement = new CashMovement
        {
            BusinessId = businessId,
            BranchId = branchId,
            CashSessionId = cashSessionId,
            MovementType = movementType,
            Amount = amount,
            Description = description,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            PaymentMethodId = paymentMethodId
        };

        if (createdBy is not null)
            movement.SetCreatedBy(createdBy);

        return movement;
    }
}

using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Inventory.GetInventoryMovements;

public sealed record GetInventoryMovementsQuery(
    Guid BusinessId,
    Guid BranchId,
    Guid ProductId,
    int Take = 50);

public sealed record InventoryMovementDto(
    Guid Id,
    DateTime CreatedAtUtc,
    MovementType MovementType,
    decimal Quantity,
    decimal PreviousQuantity,
    decimal NewQuantity,
    string? Reason,
    string? ReferenceType);

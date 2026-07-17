namespace SmallBusinessPOS.Application.Features.Inventory.AdjustInventory;

public sealed record AdjustInventoryCommand(
    Guid BusinessId,
    Guid BranchId,
    Guid ProductId,
    decimal Quantity,
    string Reason);

public sealed record AdjustInventoryResultDto(
    Guid ProductId,
    decimal PreviousQuantity,
    decimal NewQuantity);

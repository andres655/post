namespace SmallBusinessPOS.Application.Features.Inventory.SetMinimumStock;

public sealed record SetMinimumStockCommand(
    Guid BusinessId,
    Guid BranchId,
    Guid ProductId,
    decimal MinimumQuantity);

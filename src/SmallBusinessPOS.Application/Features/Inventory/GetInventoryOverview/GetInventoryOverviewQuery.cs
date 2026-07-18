namespace SmallBusinessPOS.Application.Features.Inventory.GetInventoryOverview;

public sealed record GetInventoryOverviewQuery(
    Guid BusinessId,
    Guid BranchId,
    bool LowStockOnly = false,
    string? SearchTerm = null,
    int MaxRows = 200);

public sealed record InventoryItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasure,
    decimal Quantity,
    decimal MinimumQuantity,
    bool IsLowStock);

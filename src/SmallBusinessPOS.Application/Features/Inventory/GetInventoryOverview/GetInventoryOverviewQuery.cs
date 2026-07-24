namespace SmallBusinessPOS.Application.Features.Inventory.GetInventoryOverview;

public sealed record GetInventoryOverviewQuery(
    Guid BusinessId,
    Guid BranchId,
    bool LowStockOnly = false,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 5);

public sealed record InventoryOverviewDto(
    IReadOnlyList<InventoryItemDto> Items,
    int TotalCount,
    int LowStockCount,
    int OutOfStockCount,
    decimal TotalUnits);

public sealed record InventoryItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasure,
    decimal Quantity,
    decimal MinimumQuantity,
    bool IsLowStock);

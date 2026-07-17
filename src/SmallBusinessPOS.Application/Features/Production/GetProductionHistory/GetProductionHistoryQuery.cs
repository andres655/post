namespace SmallBusinessPOS.Application.Features.Production.GetProductionHistory;

public sealed record GetProductionHistoryQuery(
    Guid BusinessId,
    Guid BranchId,
    DateOnly? From = null,
    DateOnly? To = null);

public sealed record ProductionHistoryDto(
    Guid ProductionEntryId,
    string Number,
    DateOnly ProductionDate,
    string Status,
    decimal TotalProduced,
    decimal TotalWasted,
    decimal NetAdded,
    decimal DirectCost,
    decimal InputCost,
    decimal TotalCost,
    decimal CostPerNetUnit,
    string? Notes,
    DateTime? ConfirmedAtUtc,
    IReadOnlyList<ProductionHistoryDetailDto> Details,
    IReadOnlyList<ProductionHistoryInputDto> Inputs);

public sealed record ProductionHistoryDetailDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal QuantityProduced,
    decimal QuantityWasted,
    decimal NetQuantity,
    decimal UnitCost,
    decimal TotalCost);

public sealed record ProductionHistoryInputDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal EstimatedUnitCost,
    decimal TotalCost);

namespace SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;

public sealed record ConfirmProductionEntryCommand(
    Guid? ProductionEntryId,
    Guid BusinessId,
    Guid BranchId,
    DateOnly ProductionDate,
    IReadOnlyList<ConfirmProductionEntryLine> Lines,
    string? Notes = null,
    string? Number = null);

public sealed record ConfirmProductionEntryLine(
    Guid ProductId,
    decimal QuantityProduced,
    decimal UnitCost = 0m,
    decimal QuantityWasted = 0m);

public sealed record ConfirmProductionEntryResultDto(
    Guid ProductionEntryId,
    string Number,
    DateOnly ProductionDate,
    int DetailCount,
    decimal TotalQuantityProduced,
    decimal TotalQuantityWasted,
    decimal NetQuantityAdded);

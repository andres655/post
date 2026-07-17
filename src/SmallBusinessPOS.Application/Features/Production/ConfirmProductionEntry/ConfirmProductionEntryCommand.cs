namespace SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;

public sealed record ConfirmProductionEntryCommand(
    Guid? ProductionEntryId,
    Guid BusinessId,
    Guid BranchId,
    DateOnly ProductionDate,
    IReadOnlyList<ConfirmProductionEntryLine> Lines,
    string? Notes = null,
    string? Number = null,
    IReadOnlyList<ConfirmProductionInputLine>? Inputs = null);

public sealed record ConfirmProductionEntryLine(
    Guid ProductId,
    decimal QuantityProduced,
    decimal UnitCost = 0m,
    decimal QuantityWasted = 0m);

public sealed record ConfirmProductionInputLine(
    Guid ProductId,
    decimal Quantity);

public sealed record ConfirmProductionEntryResultDto(
    Guid ProductionEntryId,
    string Number,
    DateOnly ProductionDate,
    int DetailCount,
    decimal TotalQuantityProduced,
    decimal TotalQuantityWasted,
    decimal NetQuantityAdded,
    decimal TotalInputConsumed);

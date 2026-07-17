namespace SmallBusinessPOS.Application.Features.Production.CancelProductionEntry;

public sealed record CancelProductionEntryCommand(
    Guid ProductionEntryId,
    string Reason);

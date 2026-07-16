namespace SmallBusinessPOS.Application.Features.Sales.GetCancellationHistory;

public sealed record GetCancellationHistoryQuery(
    Guid BusinessId,
    Guid BranchId,
    DateOnly FromDate,
    DateOnly ToDate,
    int MaxRows = 200);

public sealed record CancelledSaleDto(
    Guid SaleId,
    string Number,
    DateTime SoldAtUtc,
    DateTime CancelledAtUtc,
    string? CancelledBy,
    string Reason,
    decimal Total);

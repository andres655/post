namespace SmallBusinessPOS.Application.Features.Sales.GetSaleForReturn;

public sealed record GetSaleForReturnQuery(Guid BusinessId, string Number);

public sealed record SaleForReturnDto(
    Guid SaleId,
    Guid BranchId,
    Guid? CashSessionId,
    string Number,
    string Status,
    decimal Total,
    DateTime SoldAtUtc,
    string CustomerName,
    List<SaleForReturnLineDto> Lines);

public sealed record SaleForReturnLineDto(
    Guid SaleDetailId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal SoldQuantity,
    decimal ReturnedQuantity,
    decimal AvailableQuantity,
    decimal UnitPrice);

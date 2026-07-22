namespace SmallBusinessPOS.Application.Features.Sales.RegisterSaleReturn;

public sealed record RegisterSaleReturnCommand(
    Guid SaleId,
    Guid RefundPaymentMethodId,
    string Reason,
    List<RegisterSaleReturnLine> Lines,
    string? RefundReference = null,
    string? DeviceId = null);

public sealed record RegisterSaleReturnLine(Guid SaleDetailId, decimal Quantity);

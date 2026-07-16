namespace SmallBusinessPOS.Application.Features.Sales.CancelSale;

public sealed record CancelSaleCommand(Guid SaleId, string Reason, string? DeviceId = null);

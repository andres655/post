namespace SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;

public sealed record GetSaleByNumberQuery(Guid BusinessId, string Number);

public sealed record SaleLookupDto(Guid SaleId, string Number, string Status, decimal Total, DateTime SoldAtUtc);

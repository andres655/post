namespace SmallBusinessPOS.Application.Features.Sales.CreateSale;

public sealed record CreateSaleResultDto(
    Guid SaleId,
    string Number,
    decimal Subtotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    decimal Paid,
    decimal Change,
    DateTime CreatedAtUtc);

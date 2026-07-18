using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.CreateSale;

public sealed record CreateSaleCommand(
    Guid BusinessId,
    Guid BranchId,
    Guid CashRegisterId,
    SaleType SaleType,
    decimal Discount,
    decimal Tax,
    List<CreateSaleLine> Lines,
    List<CreateSalePayment> Payments,
    string? CustomerName = null,
    string? Notes = null,
    string? DeviceId = null);

public sealed record CreateSaleLine(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CreateSalePayment(
    Guid PaymentMethodId,
    decimal Amount,
    string? Reference = null,
    decimal? TenderedAmount = null);

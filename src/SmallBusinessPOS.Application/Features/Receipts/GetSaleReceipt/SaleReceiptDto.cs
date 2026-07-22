using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Receipts.GetSaleReceipt;

public sealed record SaleReceiptDto(
    Guid SaleId,
    Guid BusinessId,
    Guid BranchId,
    string ReceiptNumber,
    DateTime SoldAtUtc,
    string Status,
    string? CreatedBy,
    decimal SubTotal,
    decimal Discount,
    decimal Tax,
    decimal Total,
    string BusinessName,
    string? BusinessTaxId,
    string? BranchAddress,
    string? ReceiptPhone,
    string? ReceiptLogoPath,
    string? ReceiptHeader,
    string? TicketFooter,
    string CurrencySymbol,
    decimal PaidTotal,
    decimal Change,
    IReadOnlyList<SaleReceiptLineDto> Lines,
    IReadOnlyList<SaleReceiptPaymentDto> Payments);

public sealed record SaleReceiptLineDto(
    string ProductName,
    decimal Quantity,
    decimal LineTotal);

public sealed record SaleReceiptPaymentDto(
    string Name,
    decimal Amount,
    decimal TenderedAmount);

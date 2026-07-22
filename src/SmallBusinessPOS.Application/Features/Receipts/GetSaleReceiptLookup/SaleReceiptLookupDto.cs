namespace SmallBusinessPOS.Application.Features.Receipts.GetSaleReceiptLookup;

public sealed record SaleReceiptLookupDto(
    Guid BusinessId,
    Guid BranchId,
    Guid SaleId,
    string Number);

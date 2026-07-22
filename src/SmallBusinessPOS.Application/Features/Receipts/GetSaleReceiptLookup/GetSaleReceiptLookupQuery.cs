namespace SmallBusinessPOS.Application.Features.Receipts.GetSaleReceiptLookup;

public sealed record GetSaleReceiptLookupQuery(Guid BusinessId, Guid BranchId, Guid SaleId);

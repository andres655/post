namespace SmallBusinessPOS.Application.Features.Receipts.RegisterReceiptReprint;

public sealed record RegisterReceiptReprintCommand(
    Guid BusinessId,
    Guid BranchId,
    Guid SaleId,
    string ReceiptNumber,
    string LookupMethod,
    string ReprintedBy);

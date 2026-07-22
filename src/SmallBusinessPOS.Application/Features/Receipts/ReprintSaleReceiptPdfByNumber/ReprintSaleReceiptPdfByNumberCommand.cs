namespace SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptPdfByNumber;

public sealed record ReprintSaleReceiptPdfByNumberCommand(string Number, string ReprintedBy);

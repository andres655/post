namespace SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptPdf;

public sealed record ReprintSaleReceiptPdfCommand(Guid SaleId, string ReprintedBy);

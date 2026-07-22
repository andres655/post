namespace SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptThermal;

public sealed record ReprintSaleReceiptThermalCommand(Guid SaleId, string ReprintedBy);

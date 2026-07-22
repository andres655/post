namespace SmallBusinessPOS.Application.Features.Receipts;

public sealed record ReceiptFileDto(byte[] Content, string FileName);

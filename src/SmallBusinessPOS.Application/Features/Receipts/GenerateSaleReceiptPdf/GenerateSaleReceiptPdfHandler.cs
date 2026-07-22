using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Receipts;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Receipts.GenerateSaleReceiptPdf;

public sealed class GenerateSaleReceiptPdfHandler(IReceiptService receiptService)
{
    public async Task<Result<ReceiptFileDto>> HandleAsync(
        GenerateSaleReceiptPdfQuery query,
        CancellationToken ct = default)
    {
        var bytes = await receiptService.GenerateSaleReceiptAsync(query.SaleId, ct);
        return Result.Success(new ReceiptFileDto(bytes, $"ticket-{query.SaleId:N}.pdf"));
    }
}

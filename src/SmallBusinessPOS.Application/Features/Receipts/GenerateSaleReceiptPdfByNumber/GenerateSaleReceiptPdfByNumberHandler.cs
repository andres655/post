using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Receipts;
using SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Receipts.GenerateSaleReceiptPdfByNumber;

public sealed class GenerateSaleReceiptPdfByNumberHandler(
    GetPosContextHandler contextHandler,
    GetSaleByNumberHandler lookupHandler,
    IReceiptService receiptService)
{
    public async Task<Result<ReceiptFileDto>> HandleAsync(
        GenerateSaleReceiptPdfByNumberQuery query,
        CancellationToken ct = default)
    {
        var lookup = await FindSaleByNumberAsync(query.Number, ct);
        if (lookup.IsFailure)
            return Result.Failure<ReceiptFileDto>(lookup.Error);

        var bytes = await receiptService.GenerateSaleReceiptAsync(lookup.Value.SaleId, ct);
        return Result.Success(new ReceiptFileDto(bytes, $"ticket-{lookup.Value.Number}.pdf"));
    }

    private async Task<Result<SaleByNumberReceiptLookup>> FindSaleByNumberAsync(
        string number,
        CancellationToken ct)
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Result.Failure<SaleByNumberReceiptLookup>(contextResult.Error);

        var lookup = await lookupHandler.HandleAsync(new GetSaleByNumberQuery(
            contextResult.Value.BusinessId,
            number), ct);

        if (lookup.IsFailure)
            return Result.Failure<SaleByNumberReceiptLookup>(lookup.Error);

        return Result.Success(new SaleByNumberReceiptLookup(
            lookup.Value.SaleId,
            lookup.Value.Number));
    }

    private sealed record SaleByNumberReceiptLookup(Guid SaleId, string Number);
}

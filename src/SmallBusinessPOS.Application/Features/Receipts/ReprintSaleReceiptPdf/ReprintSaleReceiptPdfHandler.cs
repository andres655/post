using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Receipts;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceiptLookup;
using SmallBusinessPOS.Application.Features.Receipts.RegisterReceiptReprint;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptPdf;

public sealed class ReprintSaleReceiptPdfHandler(
    GetPosContextHandler contextHandler,
    GetSaleReceiptLookupHandler lookupHandler,
    RegisterReceiptReprintHandler reprintHandler,
    IReceiptService receiptService)
{
    public async Task<Result<ReceiptFileDto>> HandleAsync(
        ReprintSaleReceiptPdfCommand command,
        CancellationToken ct = default)
    {
        var lookup = await FindSaleForContextAsync(command.SaleId, ct);
        if (lookup.IsFailure)
            return Result.Failure<ReceiptFileDto>(lookup.Error);

        await RegisterReprintAsync(lookup.Value, command.ReprintedBy, "SaleId", ct);

        var bytes = await receiptService.GenerateSaleReceiptAsync(lookup.Value.SaleId, ct);
        return Result.Success(new ReceiptFileDto(bytes, $"ticket-{lookup.Value.Number}.pdf"));
    }

    private async Task<Result<SaleReceiptLookupDto>> FindSaleForContextAsync(Guid saleId, CancellationToken ct)
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Result.Failure<SaleReceiptLookupDto>(contextResult.Error);

        return await lookupHandler.HandleAsync(new GetSaleReceiptLookupQuery(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            saleId), ct);
    }

    private async Task RegisterReprintAsync(
        SaleReceiptLookupDto sale,
        string actor,
        string lookupMethod,
        CancellationToken ct)
    {
        await reprintHandler.HandleAsync(new RegisterReceiptReprintCommand(
            sale.BusinessId,
            sale.BranchId,
            sale.SaleId,
            sale.Number,
            lookupMethod,
            actor), ct);
    }
}

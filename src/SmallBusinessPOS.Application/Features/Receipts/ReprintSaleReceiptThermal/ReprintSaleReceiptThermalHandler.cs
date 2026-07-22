using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceipt;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceiptLookup;
using SmallBusinessPOS.Application.Features.Receipts.RegisterReceiptReprint;

namespace SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptThermal;

public sealed class ReprintSaleReceiptThermalHandler(
    GetPosContextHandler contextHandler,
    GetSaleReceiptLookupHandler lookupHandler,
    GetSaleReceiptHandler receiptHandler,
    RegisterReceiptReprintHandler reprintHandler)
{
    public async Task<Result<SaleReceiptDto>> HandleAsync(
        ReprintSaleReceiptThermalCommand command,
        CancellationToken ct = default)
    {
        var lookup = await FindSaleForContextAsync(command.SaleId, ct);
        if (lookup.IsFailure)
            return Result.Failure<SaleReceiptDto>(lookup.Error);

        await RegisterReprintAsync(lookup.Value, command.ReprintedBy, "SaleIdThermal", ct);

        return await receiptHandler.HandleAsync(new GetSaleReceiptQuery(lookup.Value.SaleId), ct);
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

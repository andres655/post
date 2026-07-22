using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceipt;
using SmallBusinessPOS.Application.Features.Receipts.GetSaleReceiptLookup;
using SmallBusinessPOS.Application.Features.Receipts.RegisterReceiptReprint;
using SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;

namespace SmallBusinessPOS.Application.Features.Receipts.ReprintSaleReceiptThermalByNumber;

public sealed class ReprintSaleReceiptThermalByNumberHandler(
    GetPosContextHandler contextHandler,
    GetSaleByNumberHandler lookupHandler,
    GetSaleReceiptHandler receiptHandler,
    RegisterReceiptReprintHandler reprintHandler)
{
    public async Task<Result<SaleReceiptDto>> HandleAsync(
        ReprintSaleReceiptThermalByNumberCommand command,
        CancellationToken ct = default)
    {
        var lookup = await FindSaleByNumberAsync(command.Number, ct);
        if (lookup.IsFailure)
            return Result.Failure<SaleReceiptDto>(lookup.Error);

        await RegisterReprintAsync(lookup.Value, command.ReprintedBy, "SaleNumberThermal", ct);

        return await receiptHandler.HandleAsync(new GetSaleReceiptQuery(lookup.Value.SaleId), ct);
    }

    private async Task<Result<SaleReceiptLookupDto>> FindSaleByNumberAsync(
        string number,
        CancellationToken ct)
    {
        var contextResult = await contextHandler.HandleAsync(new GetPosContextQuery(), ct);
        if (contextResult.IsFailure)
            return Result.Failure<SaleReceiptLookupDto>(contextResult.Error);

        var lookup = await lookupHandler.HandleAsync(new GetSaleByNumberQuery(
            contextResult.Value.BusinessId,
            number), ct);

        if (lookup.IsFailure)
            return Result.Failure<SaleReceiptLookupDto>(lookup.Error);

        return Result.Success(new SaleReceiptLookupDto(
            contextResult.Value.BusinessId,
            contextResult.Value.BranchId,
            lookup.Value.SaleId,
            lookup.Value.Number));
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

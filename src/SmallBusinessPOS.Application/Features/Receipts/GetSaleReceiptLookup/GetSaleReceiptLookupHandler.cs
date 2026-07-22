using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Receipts.GetSaleReceiptLookup;

public sealed class GetSaleReceiptLookupHandler(IAppDbContext db)
{
    public async Task<Result<SaleReceiptLookupDto>> HandleAsync(
        GetSaleReceiptLookupQuery query,
        CancellationToken ct = default)
    {
        var sale = await db.Sales
            .AsNoTracking()
            .Where(s => s.Id == query.SaleId && s.BusinessId == query.BusinessId)
            .Select(s => new { s.Id, s.ReceiptNumber })
            .FirstOrDefaultAsync(ct);

        if (sale is null)
            return Result.Failure<SaleReceiptLookupDto>(Error.NotFound("Sale", query.SaleId));

        return Result.Success(new SaleReceiptLookupDto(
            query.BusinessId,
            query.BranchId,
            sale.Id,
            sale.ReceiptNumber));
    }
}

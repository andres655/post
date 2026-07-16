using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Sales.GetSaleByNumber;

public sealed class GetSaleByNumberHandler(IAppDbContext db)
{
    public async Task<Result<SaleLookupDto>> HandleAsync(
        GetSaleByNumberQuery query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Number))
            return Result.Failure<SaleLookupDto>(
                Error.Validation("Number", "El número de venta es obligatorio."));

        var normalized = query.Number.Trim().ToUpperInvariant();

        var sale = await db.Sales
            .Where(s => s.BusinessId == query.BusinessId && s.ReceiptNumber == normalized)
            .Select(s => new SaleLookupDto(s.Id, s.ReceiptNumber, s.Status.ToString(), s.Total, s.SoldAtUtc))
            .FirstOrDefaultAsync(ct);

        if (sale is null)
            return Result.Failure<SaleLookupDto>(Error.NotFound("Sale", normalized));

        return Result.Success(sale);
    }
}

using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.GetCancellationHistory;

public sealed class GetCancellationHistoryHandler(IAppDbContext db)
{
    public async Task<Result<List<CancelledSaleDto>>> HandleAsync(
        GetCancellationHistoryQuery query,
        CancellationToken ct = default)
    {
        if (query.ToDate < query.FromDate)
            return Result.Failure<List<CancelledSaleDto>>(
                Error.Validation("ToDate", "La fecha final debe ser mayor o igual que la fecha inicial."));

        var fromUtc = query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = query.ToDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var rows = await db.Sales
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.Status == SaleStatus.Cancelled
                     && s.CancelledAtUtc != null
                     && s.CancelledAtUtc >= fromUtc
                     && s.CancelledAtUtc < toUtc)
            .OrderByDescending(s => s.CancelledAtUtc)
            .Take(query.MaxRows)
            .Select(s => new CancelledSaleDto(
                s.Id,
                s.ReceiptNumber,
                s.SoldAtUtc,
                s.CancelledAtUtc!.Value,
                s.CancelledBy,
                s.CancellationReason ?? "Sin motivo",
                s.Total))
            .ToListAsync(ct);

        return Result.Success(rows);
    }
}

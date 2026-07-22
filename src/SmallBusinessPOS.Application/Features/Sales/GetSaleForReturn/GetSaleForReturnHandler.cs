using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.GetSaleForReturn;

public sealed class GetSaleForReturnHandler(IAppDbContext db)
{
    public async Task<Result<SaleForReturnDto>> HandleAsync(GetSaleForReturnQuery query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Number))
            return Result.Failure<SaleForReturnDto>(Error.Validation("Number", "El numero de venta es obligatorio."));

        var normalized = query.Number.Trim().ToUpperInvariant();
        var sale = await db.Sales
            .Include(sale => sale.Details)
            .FirstOrDefaultAsync(sale => sale.BusinessId == query.BusinessId && sale.ReceiptNumber == normalized, ct);

        if (sale is null)
            return Result.Failure<SaleForReturnDto>(Error.NotFound("Sale", normalized));

        if (sale.Status is not SaleStatus.Confirmed and not SaleStatus.PartiallyRefunded)
        {
            return Result.Failure<SaleForReturnDto>(
                Error.BusinessRule("Sale.NotReturnable", "Solo se pueden devolver ventas confirmadas o parcialmente devueltas."));
        }

        var detailIds = sale.Details.Select(detail => detail.Id).ToList();
        var returnedByDetail = await db.SaleReturnDetails
            .Where(detail => detailIds.Contains(detail.SaleDetailId))
            .GroupBy(detail => detail.SaleDetailId)
            .Select(group => new { SaleDetailId = group.Key, Quantity = group.Sum(detail => detail.Quantity) })
            .ToDictionaryAsync(item => item.SaleDetailId, item => item.Quantity, ct);

        var lines = sale.Details
            .OrderBy(detail => detail.ProductName)
            .Select(detail =>
            {
                var returned = returnedByDetail.GetValueOrDefault(detail.Id);
                var available = Math.Max(0m, detail.Quantity - returned);
                return new SaleForReturnLineDto(
                    detail.Id,
                    detail.ProductId,
                    detail.ProductCode,
                    detail.ProductName,
                    detail.Quantity,
                    returned,
                    available,
                    detail.UnitPrice);
            })
            .ToList();

        return Result.Success(new SaleForReturnDto(
            sale.Id,
            sale.BranchId,
            sale.CashSessionId,
            sale.ReceiptNumber,
            sale.Status.ToString(),
            sale.Total,
            sale.SoldAtUtc,
            string.IsNullOrWhiteSpace(sale.CustomerName) ? "Consumidor final" : sale.CustomerName,
            lines));
    }
}

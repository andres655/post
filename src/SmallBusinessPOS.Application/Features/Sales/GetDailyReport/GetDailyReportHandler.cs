using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.GetDailyReport;

public sealed class GetDailyReportHandler(IAppDbContext db)
{
    public async Task<Result<DailyReportDto>> HandleAsync(GetDailyReportQuery query, CancellationToken ct = default)
    {
        var fromUtc = query.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(1);

        var sales = await db.Sales
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.SoldAtUtc >= fromUtc
                     && s.SoldAtUtc < toUtc)
            .OrderByDescending(s => s.SoldAtUtc)
            .ToListAsync(ct);

        var confirmedSales = sales.Where(s => s.Status == SaleStatus.Confirmed).ToList();
        var cancelledSales = sales.Where(s => s.Status == SaleStatus.Cancelled).ToList();

        var grossSales = confirmedSales.Sum(s => s.Total);
        var discounts = confirmedSales.Sum(s => s.Discount);
        var cancelledAmount = cancelledSales.Sum(s => s.Total);
        var netSales = grossSales - cancelledAmount;

        var saleIds = confirmedSales.Select(s => s.Id).ToList();

        var payments = await db.SalePayments
            .Include(p => p.PaymentMethod)
            .Where(p => saleIds.Contains(p.SaleId))
            .ToListAsync(ct);

        var salesByPayment = payments
            .GroupBy(p => p.PaymentMethod.Name)
            .Select(g => new DailyPaymentSummaryDto(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var saleDetails = await db.SaleDetails
            .Where(d => saleIds.Contains(d.SaleId))
            .ToListAsync(ct);

        var topProducts = saleDetails
            .GroupBy(d => new { d.ProductCode, d.ProductName })
            .Select(g => new DailyTopProductDto(
                g.Key.ProductCode,
                g.Key.ProductName,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal)))
            .OrderByDescending(x => x.Quantity)
            .Take(10)
            .ToList();

        var expenses = await db.Expenses
            .Where(e => e.BusinessId == query.BusinessId
                     && e.BranchId == query.BranchId
                     && e.CreatedAtUtc >= fromUtc
                     && e.CreatedAtUtc < toUtc)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var cashSales = salesByPayment
            .Where(x => x.PaymentMethod.Contains("efectivo", StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Amount);

        var expectedCash = await db.CashSessions
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.OpenedAtUtc >= fromUtc
                     && s.OpenedAtUtc < toUtc)
            .SumAsync(s => (decimal?)s.ClosingBalance, ct) ?? 0m;

        var pollo = await db.Products
            .Where(p => p.BusinessId == query.BusinessId && p.Code == "POL-ENT")
            .Select(p => new { p.Id, p.Name })
            .FirstOrDefaultAsync(ct);

        var pollosPrepared = 0m;
        var pollosSoldEquivalent = 0m;
        var pollosAvailable = 0m;
        var waste = 0m;

        if (pollo is not null)
        {
            pollosPrepared = await db.InventoryMovements
                .Where(m => m.ProductId == pollo.Id
                         && m.MovementType == MovementType.ProductionOutput
                         && m.CreatedAtUtc >= fromUtc
                         && m.CreatedAtUtc < toUtc)
                .SumAsync(m => (decimal?)m.Quantity, ct) ?? 0m;

            pollosSoldEquivalent = await db.InventoryMovements
                .Where(m => m.ProductId == pollo.Id
                         && m.MovementType == MovementType.Sale
                         && m.CreatedAtUtc >= fromUtc
                         && m.CreatedAtUtc < toUtc)
                .SumAsync(m => (decimal?)m.Quantity, ct) ?? 0m;

            pollosAvailable = await db.InventoryStocks
                .Where(s => s.BusinessId == query.BusinessId
                         && s.BranchId == query.BranchId
                         && s.ProductId == pollo.Id)
                .Select(s => (decimal?)s.Quantity)
                .FirstOrDefaultAsync(ct) ?? 0m;

            waste = await db.InventoryMovements
                .Where(m => m.ProductId == pollo.Id
                         && m.MovementType == MovementType.Waste
                         && m.CreatedAtUtc >= fromUtc
                         && m.CreatedAtUtc < toUtc)
                .SumAsync(m => (decimal?)m.Quantity, ct) ?? 0m;
        }

        var lowStock = await db.InventoryStocks
            .Include(s => s.Product)
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.Product.IsActive
                     && s.Quantity <= s.MinimumQuantity)
            .OrderBy(s => s.Quantity)
            .Take(20)
            .Select(s => new DailyLowStockDto(s.Product.Code, s.Product.Name, s.Quantity, s.MinimumQuantity))
            .ToListAsync(ct);

        var saleRows = sales
            .Select(s => new DailySaleSummaryDto(s.Id, s.ReceiptNumber, s.SoldAtUtc, s.Status.ToString(), s.Total))
            .ToList();

        return Result.Success(new DailyReportDto(
            query.Date,
            grossSales,
            discounts,
            cancelledAmount,
            netSales,
            expenses,
            confirmedSales.Count,
            cashSales,
            expectedCash,
            pollosPrepared,
            pollosSoldEquivalent,
            pollosAvailable,
            waste,
            salesByPayment,
            topProducts,
            saleRows,
            lowStock));
    }
}

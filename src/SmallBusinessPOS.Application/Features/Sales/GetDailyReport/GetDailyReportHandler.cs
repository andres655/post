using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.GetDailyReport;

public sealed class GetDailyReportHandler(IAppDbContext db)
{
    public async Task<Result<DailyReportDto>> HandleAsync(GetDailyReportQuery query, CancellationToken ct = default)
    {
        var (fromUtc, toUtc) = await GetUtcDateRangeAsync(query.BusinessId, query.Date, ct);

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

        var soldProductIds = saleDetails.Select(d => d.ProductId).Distinct().ToList();
        var soldProducts = await db.Products
            .Where(p => soldProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var componentCosts = await BuildComponentCostMapAsync(soldProductIds, ct);

        var topProducts = saleDetails
            .GroupBy(d => new { d.ProductId, d.ProductCode, d.ProductName })
            .Select(g =>
            {
                var quantity = g.Sum(x => x.Quantity);
                var salesAmount = g.Sum(x => x.LineTotal);
                var unitCost = ResolveUnitCost(g.Key.ProductId, soldProducts, componentCosts);
                var estimatedCost = quantity * unitCost;
                var grossMargin = salesAmount - estimatedCost;

                return new DailyTopProductDto(
                    g.Key.ProductCode,
                    g.Key.ProductName,
                    quantity,
                    salesAmount,
                    estimatedCost,
                    grossMargin,
                    salesAmount == 0m ? 0m : grossMargin / salesAmount * 100m);
            })
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

    private async Task<(DateTime FromUtc, DateTime ToUtc)> GetUtcDateRangeAsync(
        Guid businessId,
        DateOnly date,
        CancellationToken ct)
    {
        var timeZoneId = await db.Businesses
            .Where(b => b.Id == businessId)
            .Select(b => b.TimeZone)
            .FirstOrDefaultAsync(ct);

        var timeZone = ResolveTimeZone(timeZoneId);
        var fromLocal = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var toLocal = fromLocal.AddDays(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(fromLocal, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(toLocal, timeZone));
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private async Task<Dictionary<Guid, decimal>> BuildComponentCostMapAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken ct)
    {
        var components = await db.ProductComponents
            .Include(c => c.ComponentProduct)
            .Where(c => productIds.Contains(c.ParentProductId))
            .ToListAsync(ct);

        return components
            .GroupBy(c => c.ParentProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(c => c.Quantity * c.ComponentProduct.EstimatedCost));
    }

    private static decimal ResolveUnitCost(
        Guid productId,
        IReadOnlyDictionary<Guid, Product> products,
        IReadOnlyDictionary<Guid, decimal> componentCosts)
    {
        if (products.TryGetValue(productId, out var product) && product.EstimatedCost > 0m)
            return product.EstimatedCost;

        return componentCosts.TryGetValue(productId, out var componentCost)
            ? componentCost
            : 0m;
    }
}

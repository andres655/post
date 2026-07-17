using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Reports.GetProfitabilityReport;

public sealed class GetProfitabilityReportHandler(IAppDbContext db)
{
    public async Task<Result<ProfitabilityReportDto>> HandleAsync(
        GetProfitabilityReportQuery query,
        CancellationToken ct = default)
    {
        if (query.To < query.From)
            return Result.Failure<ProfitabilityReportDto>(
                Error.Validation("To", "La fecha final no puede ser menor que la fecha inicial."));

        var fromUtc = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = query.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var sales = await db.Sales
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.SoldAtUtc >= fromUtc
                     && s.SoldAtUtc < toUtc)
            .ToListAsync(ct);

        var confirmedSales = sales.Where(s => s.Status == SaleStatus.Confirmed).ToList();
        var cancelledAmount = sales
            .Where(s => s.Status == SaleStatus.Cancelled)
            .Sum(s => s.Total);

        var saleIds = confirmedSales.Select(s => s.Id).ToList();
        var saleDetails = await db.SaleDetails
            .Where(d => saleIds.Contains(d.SaleId))
            .ToListAsync(ct);

        var soldProductIds = saleDetails.Select(d => d.ProductId).Distinct().ToList();
        var soldProducts = await db.Products
            .Where(p => soldProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var componentCosts = await BuildComponentCostMapAsync(soldProductIds, ct);
        var saleDateById = confirmedSales.ToDictionary(s => s.Id, s => DateOnly.FromDateTime(s.SoldAtUtc));

        var productRows = saleDetails
            .GroupBy(d => new { d.ProductId, d.ProductCode, d.ProductName })
            .Select(g =>
            {
                var quantity = g.Sum(x => x.Quantity);
                var salesAmount = g.Sum(x => x.LineTotal);
                var unitCost = ResolveUnitCost(g.Key.ProductId, soldProducts, componentCosts);
                var estimatedCost = quantity * unitCost;
                var grossMargin = salesAmount - estimatedCost;

                return new ProfitabilityProductDto(
                    g.Key.ProductCode,
                    g.Key.ProductName,
                    quantity,
                    salesAmount,
                    estimatedCost,
                    grossMargin,
                    salesAmount == 0m ? 0m : grossMargin / salesAmount * 100m);
            })
            .OrderByDescending(x => x.GrossMargin)
            .ThenByDescending(x => x.SalesAmount)
            .ToList();

        var expenses = await db.Expenses
            .Where(e => e.BusinessId == query.BusinessId
                     && e.BranchId == query.BranchId
                     && e.CreatedAtUtc >= fromUtc
                     && e.CreatedAtUtc < toUtc)
            .ToListAsync(ct);

        var dailySales = saleDetails
            .GroupBy(d => saleDateById[d.SaleId])
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var salesAmount = g.Sum(x => x.LineTotal);
                    var estimatedCost = g.Sum(x =>
                        x.Quantity * ResolveUnitCost(x.ProductId, soldProducts, componentCosts));
                    return new { SalesAmount = salesAmount, EstimatedCost = estimatedCost };
                });

        var dailyExpenses = expenses
            .GroupBy(e => DateOnly.FromDateTime(e.CreatedAtUtc))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var daily = new List<ProfitabilityDailyDto>();
        for (var date = query.From; date <= query.To; date = date.AddDays(1))
        {
            dailySales.TryGetValue(date, out var salesForDay);
            dailyExpenses.TryGetValue(date, out var expensesForDay);

            var salesAmount = salesForDay?.SalesAmount ?? 0m;
            var estimatedCost = salesForDay?.EstimatedCost ?? 0m;
            var grossMargin = salesAmount - estimatedCost;

            daily.Add(new ProfitabilityDailyDto(
                date,
                salesAmount,
                estimatedCost,
                grossMargin,
                expensesForDay,
                grossMargin - expensesForDay));
        }

        var grossSales = confirmedSales.Sum(s => s.Total);
        var estimatedTotalCost = productRows.Sum(p => p.EstimatedCost);
        var grossTotalMargin = grossSales - estimatedTotalCost;
        var expenseTotal = expenses.Sum(e => e.Amount);

        return Result.Success(new ProfitabilityReportDto(
            query.From,
            query.To,
            grossSales,
            cancelledAmount,
            grossSales,
            estimatedTotalCost,
            grossTotalMargin,
            grossSales == 0m ? 0m : grossTotalMargin / grossSales * 100m,
            expenseTotal,
            grossTotalMargin - expenseTotal,
            productRows,
            daily));
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

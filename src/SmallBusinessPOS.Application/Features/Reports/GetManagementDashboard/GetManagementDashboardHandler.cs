using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Reports.GetManagementDashboard;

public sealed class GetManagementDashboardHandler(IAppDbContext db)
{
    public async Task<Result<ManagementDashboardDto>> HandleAsync(
        GetManagementDashboardQuery query,
        CancellationToken ct = default)
    {
        var todayFrom = query.Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var tomorrow = todayFrom.AddDays(1);
        var weekFrom = todayFrom.AddDays(-6);

        var sales = await db.Sales
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.SoldAtUtc >= weekFrom
                     && s.SoldAtUtc < tomorrow)
            .ToListAsync(ct);

        var confirmedSales = sales.Where(s => s.Status == SaleStatus.Confirmed).ToList();
        var todaySales = confirmedSales
            .Where(s => s.SoldAtUtc >= todayFrom && s.SoldAtUtc < tomorrow)
            .ToList();

        var saleIds = confirmedSales.Select(s => s.Id).ToList();
        var saleDetails = await db.SaleDetails
            .Where(d => saleIds.Contains(d.SaleId))
            .ToListAsync(ct);

        var soldProductIds = saleDetails.Select(d => d.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => soldProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var componentCosts = await BuildComponentCostMapAsync(soldProductIds, ct);
        var saleDateById = confirmedSales.ToDictionary(s => s.Id, s => DateOnly.FromDateTime(s.SoldAtUtc));

        var todaySaleIds = todaySales.Select(s => s.Id).ToHashSet();
        var todayDetails = saleDetails.Where(d => todaySaleIds.Contains(d.SaleId)).ToList();
        var todayCost = todayDetails.Sum(d => d.Quantity * ResolveUnitCost(d.ProductId, products, componentCosts));
        var todaySalesAmount = todaySales.Sum(s => s.Total);
        var todayGrossMargin = todaySalesAmount - todayCost;
        var todayGrossMarginPercent = todaySalesAmount == 0m
            ? 0m
            : todayGrossMargin / todaySalesAmount * 100m;

        var weekSalesAmount = confirmedSales.Sum(s => s.Total);
        var weekCost = saleDetails.Sum(d => d.Quantity * ResolveUnitCost(d.ProductId, products, componentCosts));
        var weekGrossMargin = weekSalesAmount - weekCost;

        var todayExpenses = await db.Expenses
            .Where(e => e.BusinessId == query.BusinessId
                     && e.BranchId == query.BranchId
                     && e.CreatedAtUtc >= todayFrom
                     && e.CreatedAtUtc < tomorrow)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var weekExpenses = await db.Expenses
            .Where(e => e.BusinessId == query.BusinessId
                     && e.BranchId == query.BranchId
                     && e.CreatedAtUtc >= weekFrom
                     && e.CreatedAtUtc < tomorrow)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var openCash = await db.CashSessions
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.Status == CashSessionStatus.Open)
            .OrderByDescending(s => s.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        var lowStockCount = await db.InventoryStocks
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.Product.IsActive
                     && s.MinimumQuantity > 0m
                     && s.Quantity <= s.MinimumQuantity)
            .CountAsync(ct);

        var lowStockItems = await db.InventoryStocks
            .Include(s => s.Product)
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && s.Product.IsActive
                     && s.MinimumQuantity > 0m
                     && s.Quantity <= s.MinimumQuantity)
            .OrderBy(s => s.Quantity)
            .ThenBy(s => s.Product.Code)
            .Take(8)
            .Select(s => new DashboardLowStockDto(
                s.Product.Code,
                s.Product.Name,
                s.Quantity,
                s.MinimumQuantity))
            .ToListAsync(ct);

        var pollo = await db.Products
            .Where(p => p.BusinessId == query.BusinessId && p.Code == "POL-ENT")
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(ct);

        var pollosAvailable = 0m;
        var pollosPreparedToday = 0m;
        var pollosSoldToday = 0m;
        var wasteToday = 0m;
        if (pollo is not null)
        {
            pollosAvailable = await db.InventoryStocks
                .Where(s => s.BusinessId == query.BusinessId
                         && s.BranchId == query.BranchId
                         && s.ProductId == pollo.Id)
                .Select(s => (decimal?)s.Quantity)
                .FirstOrDefaultAsync(ct) ?? 0m;

            pollosPreparedToday = await db.InventoryMovements
                .Where(m => m.BusinessId == query.BusinessId
                         && m.BranchId == query.BranchId
                         && m.ProductId == pollo.Id
                         && m.MovementType == MovementType.ProductionOutput
                         && m.CreatedAtUtc >= todayFrom
                         && m.CreatedAtUtc < tomorrow)
                .SumAsync(m => (decimal?)m.Quantity, ct) ?? 0m;

            pollosSoldToday = await db.InventoryMovements
                .Where(m => m.BusinessId == query.BusinessId
                         && m.BranchId == query.BranchId
                         && m.ProductId == pollo.Id
                         && m.MovementType == MovementType.Sale
                         && m.CreatedAtUtc >= todayFrom
                         && m.CreatedAtUtc < tomorrow)
                .SumAsync(m => (decimal?)m.Quantity, ct) ?? 0m;

            wasteToday = await db.InventoryMovements
                .Where(m => m.BusinessId == query.BusinessId
                         && m.BranchId == query.BranchId
                         && m.ProductId == pollo.Id
                         && m.MovementType == MovementType.Waste
                         && m.CreatedAtUtc >= todayFrom
                         && m.CreatedAtUtc < tomorrow)
                .SumAsync(m => (decimal?)m.Quantity, ct) ?? 0m;
        }

        var topProducts = todayDetails
            .GroupBy(d => new { d.ProductId, d.ProductCode, d.ProductName })
            .Select(g =>
            {
                var quantity = g.Sum(x => x.Quantity);
                var salesAmount = g.Sum(x => x.LineTotal);
                var cost = quantity * ResolveUnitCost(g.Key.ProductId, products, componentCosts);

                return new DashboardProductDto(
                    g.Key.ProductCode,
                    g.Key.ProductName,
                    quantity,
                    salesAmount,
                    salesAmount - cost);
            })
            .OrderByDescending(x => x.SalesAmount)
            .Take(5)
            .ToList();

        var recentSales = sales
            .OrderByDescending(s => s.SoldAtUtc)
            .Take(8)
            .Select(s => new DashboardActivityDto(
                s.SoldAtUtc,
                "Ventas",
                s.Status == SaleStatus.Cancelled ? "Venta anulada" : "Venta registrada",
                s.ReceiptNumber,
                s.Total));

        var recentInventory = await db.InventoryMovements
            .Include(m => m.Product)
            .Where(m => m.BusinessId == query.BusinessId
                     && m.BranchId == query.BranchId
                     && m.CreatedAtUtc >= weekFrom
                     && m.CreatedAtUtc < tomorrow)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(8)
            .Select(m => new DashboardActivityDto(
                m.CreatedAtUtc,
                "Inventario",
                m.MovementType.ToString(),
                m.Product.Code,
                m.Quantity))
            .ToListAsync(ct);

        var recentActivity = recentSales
            .Concat(recentInventory)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(10)
            .ToList();

        var kpis = new DashboardKpiDto(
            todaySalesAmount,
            todaySales.Count,
            todayGrossMargin,
            todayExpenses,
            todayGrossMargin - todayExpenses,
            openCash?.ClosingBalance ?? 0m,
            openCash is not null,
            weekSalesAmount,
            weekGrossMargin - weekExpenses,
            lowStockCount,
            pollosAvailable,
            pollosPreparedToday,
            pollosSoldToday,
            wasteToday,
            todayGrossMarginPercent);

        return Result.Success(new ManagementDashboardDto(query.Today, kpis, topProducts, recentActivity, lowStockItems));
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

using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Inventory.GetInventoryOverview;

public sealed class GetInventoryOverviewHandler(IAppDbContext db)
{
    public async Task<Result<IReadOnlyList<InventoryItemDto>>> HandleAsync(
        GetInventoryOverviewQuery query,
        CancellationToken ct = default)
    {
        var products = await db.Products
            .Where(p => p.BusinessId == query.BusinessId
                     && p.IsActive
                     && p.TracksInventory)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            products = products
                .Where(p => p.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                         || p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var productIds = products.Select(p => p.Id).ToList();
        var stocks = await db.InventoryStocks
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, ct);

        var items = products.Select(product =>
        {
            stocks.TryGetValue(product.Id, out var stock);
            var quantity = stock?.Quantity ?? 0m;
            var minimum = stock?.MinimumQuantity ?? 0m;
            var isLow = minimum > 0m && quantity <= minimum;

            return new InventoryItemDto(
                product.Id,
                product.Code,
                product.Name,
                product.UnitOfMeasure.ToString(),
                quantity,
                minimum,
                isLow);
        });

        if (query.LowStockOnly)
            items = items.Where(i => i.IsLowStock);

        return Result.Success<IReadOnlyList<InventoryItemDto>>(items.ToList());
    }
}

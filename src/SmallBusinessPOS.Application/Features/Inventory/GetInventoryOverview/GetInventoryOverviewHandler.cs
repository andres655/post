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
        var take = Math.Clamp(query.MaxRows, 1, 500);

        var productsQuery = db.Products
            .Where(p => p.BusinessId == query.BusinessId
                     && p.IsActive
                     && p.TracksInventory);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            productsQuery = productsQuery.Where(p =>
                p.Code.ToLower().Contains(term) ||
                p.Name.ToLower().Contains(term));
        }

        var products = await productsQuery
            .OrderBy(p => p.Code)
            .Take(take)
            .ToListAsync(ct);

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

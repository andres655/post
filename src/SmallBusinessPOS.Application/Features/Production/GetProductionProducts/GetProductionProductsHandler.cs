using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Production.GetProductionProducts;

public sealed class GetProductionProductsHandler(IAppDbContext db)
{
    public async Task<Result<List<ProductionProductDto>>> HandleAsync(
        GetProductionProductsQuery query,
        CancellationToken ct = default)
    {
        var products = await db.Products
            .Where(p => p.BusinessId == query.BusinessId
                     && p.IsActive
                     && p.TracksInventory
                     && p.ProductType == ProductType.PreparedItem)
            .OrderBy(p => p.Name)
            .Select(p => new ProductionProductDto(
                p.Id,
                p.Code,
                p.Name,
                p.EstimatedCost,
                p.SalePrice))
            .ToListAsync(ct);

        return Result.Success(products);
    }
}

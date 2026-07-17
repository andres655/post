using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Production.GetProductionInputProducts;

public sealed class GetProductionInputProductsHandler(IAppDbContext db)
{
    public async Task<Result<List<ProductionInputProductDto>>> HandleAsync(
        GetProductionInputProductsQuery query,
        CancellationToken ct = default)
    {
        var products = await db.Products
            .Where(p => p.BusinessId == query.BusinessId
                     && p.IsActive
                     && p.TracksInventory
                     && p.ProductType != ProductType.Service
                     && p.ProductType != ProductType.Combo)
            .OrderBy(p => p.Code)
            .Select(p => new ProductionInputProductDto(
                p.Id,
                p.Code,
                p.Name))
            .ToListAsync(ct);

        return Result.Success(products);
    }
}

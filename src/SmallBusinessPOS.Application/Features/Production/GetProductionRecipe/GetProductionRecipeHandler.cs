using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Production.GetProductionRecipe;

public sealed class GetProductionRecipeHandler(IAppDbContext db)
{
    public async Task<Result<ProductionRecipeDto>> HandleAsync(
        GetProductionRecipeQuery query,
        CancellationToken ct = default)
    {
        var parent = await db.Products
            .FirstOrDefaultAsync(p => p.Id == query.ParentProductId && p.BusinessId == query.BusinessId, ct);

        if (parent is null)
            return Result.Failure<ProductionRecipeDto>(Error.NotFound("Product", query.ParentProductId));

        var components = await db.ProductComponents
            .Include(c => c.ComponentProduct)
            .Where(c => c.ParentProductId == query.ParentProductId)
            .OrderBy(c => c.ComponentProduct.Code)
            .Select(c => new ProductionRecipeComponentDto(
                c.ComponentProductId,
                c.ComponentProduct.Code,
                c.ComponentProduct.Name,
                c.Quantity))
            .ToListAsync(ct);

        return Result.Success(new ProductionRecipeDto(query.ParentProductId, components));
    }
}

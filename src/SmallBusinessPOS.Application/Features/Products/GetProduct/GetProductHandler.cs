using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Products.GetProduct;

public sealed class GetProductHandler(IAppDbContext db)
{
    public async Task<Result<ProductDto>> HandleAsync(
        GetProductQuery query,
        CancellationToken ct = default)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Components)
                .ThenInclude(c => c.ComponentProduct)
            .FirstOrDefaultAsync(p => p.Id == query.Id, ct);

        if (product is null)
            return Result.Failure<ProductDto>(Error.NotFound("Product", query.Id));

        var inventoryComponents = product.Components
            .Select(component => new ProductInventoryComponentDto(
                component.ComponentProductId,
                component.ComponentProduct.Code,
                component.ComponentProduct.Name,
                component.Quantity))
            .ToList();

        return Result.Success(
            CreateProduct.CreateProductHandler.MapToDto(
                product,
                product.Category?.Name,
                inventoryComponents));
    }
}

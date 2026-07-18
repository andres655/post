using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Products.GetProducts;

public sealed class GetProductsHandler(IAppDbContext db)
{
    public async Task<Result<List<ProductSummaryDto>>> HandleAsync(
        GetProductsQuery query,
        CancellationToken ct = default)
    {
        var productsQuery = db.Products
            .Include(p => p.Category)
            .Where(p => p.BusinessId == query.BusinessId);

        if (query.OnlyActive)
            productsQuery = productsQuery.Where(p => p.IsActive);

        if (query.CategoryId.HasValue)
            productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId.Value);

        if (query.ProductType.HasValue)
            productsQuery = productsQuery.Where(p => p.ProductType == query.ProductType.Value);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToLower();
            productsQuery = productsQuery.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Code.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }

        var take = Math.Clamp(query.MaxRows, 1, 500);

        var products = await productsQuery
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Code,
                p.Name,
                p.Category != null ? p.Category.Name : null,
                p.ProductType,
                p.SalePrice,
                p.IsActive))
            .ToListAsync(ct);

        return Result.Success(products);
    }
}

using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Categories.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Categories.GetCategories;

public sealed class GetCategoriesHandler(IAppDbContext db)
{
    public async Task<Result<List<CategoryDto>>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken ct = default)
    {
        var categoriesQuery = db.Categories
            .Where(c => c.BusinessId == query.BusinessId);

        if (query.OnlyActive)
            categoriesQuery = categoriesQuery.Where(c => c.IsActive);

        var categories = await categoriesQuery
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                Category = c,
                ProductCount = db.Products.Count(p => p.CategoryId == c.Id && p.IsActive)
            })
            .ToListAsync(ct);

        var dtos = categories
            .Select(x => CreateCategory.CreateCategoryHandler.MapToDto(x.Category, x.ProductCount))
            .ToList();

        return Result.Success(dtos);
    }
}

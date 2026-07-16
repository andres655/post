using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Categories.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Categories.GetCategory;

public sealed class GetCategoryHandler(IAppDbContext db)
{
    public async Task<Result<CategoryDto>> HandleAsync(
        GetCategoryQuery query,
        CancellationToken ct = default)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == query.Id, ct);

        if (category is null)
            return Result.Failure<CategoryDto>(Error.NotFound("Category", query.Id));

        var productCount = await db.Products
            .CountAsync(p => p.CategoryId == category.Id && p.IsActive, ct);

        return Result.Success(CreateCategory.CreateCategoryHandler.MapToDto(category, productCount));
    }
}

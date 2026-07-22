using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.ExpenseCategories.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.ExpenseCategories.GetExpenseCategories;

public sealed class GetExpenseCategoriesHandler(IAppDbContext db)
{
    public async Task<Result<List<ExpenseCategoryDto>>> HandleAsync(
        GetExpenseCategoriesQuery query,
        CancellationToken ct = default)
    {
        var take = Math.Clamp(query.MaxRows, 1, 500);

        var categoriesQuery = db.ExpenseCategories
            .Where(c => c.BusinessId == query.BusinessId);

        if (query.OnlyActive)
            categoriesQuery = categoriesQuery.Where(c => c.IsActive);

        var categories = await categoriesQuery
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Take(take)
            .Select(c => new ExpenseCategoryDto(
                c.Id,
                c.BusinessId,
                c.Name,
                c.Description,
                c.SortOrder,
                c.IsActive))
            .ToListAsync(ct);

        return Result.Success(categories);
    }
}

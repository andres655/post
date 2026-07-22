using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.ExpenseCategories.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.ExpenseCategories.CreateExpenseCategory;

public sealed class CreateExpenseCategoryHandler(
    IAppDbContext db,
    CreateExpenseCategoryValidator validator)
{
    public async Task<Result<ExpenseCategoryDto>> HandleAsync(
        CreateExpenseCategoryCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0];
            return Result.Failure<ExpenseCategoryDto>(
                Error.Validation(firstError.PropertyName, firstError.ErrorMessage));
        }

        var exists = await db.ExpenseCategories
            .AnyAsync(c => c.BusinessId == command.BusinessId
                        && c.Name == command.Name.Trim(), ct);

        if (exists)
            return Result.Failure<ExpenseCategoryDto>(
                Error.Conflict("ExpenseCategory.DuplicateName",
                    $"Ya existe una categoria de gastos llamada '{command.Name}'."));

        var category = ExpenseCategory.Create(
            command.BusinessId,
            command.Name,
            command.Description,
            command.SortOrder,
            currentUser);

        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync(ct);

        return Result.Success(new ExpenseCategoryDto(
            category.Id,
            category.BusinessId,
            category.Name,
            category.Description,
            category.SortOrder,
            category.IsActive));
    }
}

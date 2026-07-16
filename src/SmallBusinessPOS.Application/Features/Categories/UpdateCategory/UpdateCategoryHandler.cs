using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Categories.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    IAppDbContext db,
    UpdateCategoryValidator validator)
{
    public async Task<Result<CategoryDto>> HandleAsync(
        UpdateCategoryCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0];
            return Result.Failure<CategoryDto>(
                Error.Validation(firstError.PropertyName, firstError.ErrorMessage));
        }

        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == command.Id, ct);

        if (category is null)
            return Result.Failure<CategoryDto>(
                Error.NotFound("Category", command.Id));

        // Verificar nombre único por negocio (excluyendo la misma categoría)
        var nameExists = await db.Categories
            .AnyAsync(c => c.BusinessId == category.BusinessId
                        && c.Name == command.Name.Trim()
                        && c.Id != command.Id
                        && c.IsActive, ct);

        if (nameExists)
            return Result.Failure<CategoryDto>(
                Error.Conflict("Category.DuplicateName",
                    $"Ya existe otra categoría activa con el nombre '{command.Name}' en este negocio."));

        category.Update(command.Name, command.Description, command.SortOrder);

        if (currentUser is not null)
            category.SetUpdated(currentUser);

        await db.SaveChangesAsync(ct);

        var productCount = await db.Products
            .CountAsync(p => p.CategoryId == category.Id && p.IsActive, ct);

        return Result.Success(CreateCategory.CreateCategoryHandler.MapToDto(category, productCount));
    }
}

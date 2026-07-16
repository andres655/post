using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Categories.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler(
    IAppDbContext db,
    CreateCategoryValidator validator)
{
    public async Task<Result<CategoryDto>> HandleAsync(
        CreateCategoryCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        // Validar el comando
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0];
            return Result.Failure<CategoryDto>(
                Error.Validation(firstError.PropertyName, firstError.ErrorMessage));
        }

        // Verificar que el negocio exista
        var businessExists = await db.Businesses
            .AnyAsync(b => b.Id == command.BusinessId && b.IsActive, ct);

        if (!businessExists)
            return Result.Failure<CategoryDto>(
                Error.NotFound(nameof(Business), command.BusinessId));

        // Verificar nombre único por negocio
        var nameExists = await db.Categories
            .AnyAsync(c => c.BusinessId == command.BusinessId
                        && c.Name == command.Name.Trim()
                        && c.IsActive, ct);

        if (nameExists)
            return Result.Failure<CategoryDto>(
                Error.Conflict("Category.DuplicateName",
                    $"Ya existe una categoría activa con el nombre '{command.Name}' en este negocio."));

        var category = Category.Create(
            command.BusinessId,
            command.Name,
            command.Description,
            command.SortOrder);

        if (currentUser is not null)
            category.SetCreatedBy(currentUser);

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return Result.Success(MapToDto(category, 0));
    }

    internal static CategoryDto MapToDto(Category c, int productCount) =>
        new(c.Id, c.BusinessId, c.Name, c.Description, c.SortOrder,
            c.IsActive, productCount, c.CreatedAtUtc, c.UpdatedAtUtc);
}

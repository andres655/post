using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    IAppDbContext db,
    UpdateProductValidator validator)
{
    public async Task<Result<ProductDto>> HandleAsync(
        UpdateProductCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var firstError = validation.Errors[0];
            return Result.Failure<ProductDto>(
                Error.Validation(firstError.PropertyName, firstError.ErrorMessage));
        }

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == command.Id, ct);

        if (product is null)
            return Result.Failure<ProductDto>(Error.NotFound("Product", command.Id));

        // Código único por negocio (excluyendo el mismo producto)
        var codeExists = await db.Products
            .AnyAsync(p => p.BusinessId == product.BusinessId
                        && p.Code == command.Code.Trim().ToUpperInvariant()
                        && p.Id != command.Id, ct);

        if (codeExists)
            return Result.Failure<ProductDto>(
                Error.Conflict("Product.DuplicateCode",
                    $"Ya existe otro producto con el código '{command.Code}' en este negocio."));

        // Verificar categoría si se proporcionó
        string? categoryName = null;
        if (command.CategoryId.HasValue)
        {
            var category = await db.Categories
                .FirstOrDefaultAsync(c => c.Id == command.CategoryId.Value && c.BusinessId == product.BusinessId, ct);

            if (category is null)
                return Result.Failure<ProductDto>(Error.NotFound("Category", command.CategoryId.Value));

            categoryName = category.Name;
        }

        product.Update(
            command.Code, command.Name, command.Description,
            command.ProductType, command.UnitOfMeasure,
            command.SalePrice, command.EstimatedCost,
            command.CategoryId, command.TracksInventory,
            command.AllowsFractionalQuantity, command.Barcode);

        if (currentUser is not null)
            product.SetUpdated(currentUser);

        await db.SaveChangesAsync(ct);

        return Result.Success(CreateProduct.CreateProductHandler.MapToDto(product, categoryName));
    }
}

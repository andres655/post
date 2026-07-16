using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler(
    IAppDbContext db,
    CreateProductValidator validator)
{
    public async Task<Result<ProductDto>> HandleAsync(
        CreateProductCommand command,
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

        // Verificar que el negocio exista
        var businessExists = await db.Businesses
            .AnyAsync(b => b.Id == command.BusinessId && b.IsActive, ct);

        if (!businessExists)
            return Result.Failure<ProductDto>(
                Error.NotFound(nameof(Business), command.BusinessId));

        // Código único por negocio
        var codeExists = await db.Products
            .AnyAsync(p => p.BusinessId == command.BusinessId
                        && p.Code == command.Code.Trim().ToUpperInvariant(), ct);

        if (codeExists)
            return Result.Failure<ProductDto>(
                Error.Conflict("Product.DuplicateCode",
                    $"Ya existe un producto con el código '{command.Code}' en este negocio."));

        // Verificar categoría si se proporcionó
        string? categoryName = null;
        if (command.CategoryId.HasValue)
        {
            var category = await db.Categories
                .FirstOrDefaultAsync(c => c.Id == command.CategoryId.Value && c.BusinessId == command.BusinessId, ct);

            if (category is null)
                return Result.Failure<ProductDto>(
                    Error.NotFound("Category", command.CategoryId.Value));

            categoryName = category.Name;
        }

        var product = Product.Create(
            command.BusinessId,
            command.Code,
            command.Name,
            command.ProductType,
            command.UnitOfMeasure,
            command.SalePrice,
            command.EstimatedCost,
            command.CategoryId,
            command.TracksInventory,
            command.AllowsFractionalQuantity,
            command.Description,
            command.Barcode);

        if (currentUser is not null)
            product.SetCreatedBy(currentUser);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return Result.Success(MapToDto(product, categoryName));
    }

    internal static ProductDto MapToDto(Product p, string? categoryName) =>
        new(p.Id, p.BusinessId, p.CategoryId, categoryName,
            p.Code, p.Barcode, p.Name, p.Description,
            p.ProductType, GetProductTypeName(p.ProductType),
            p.UnitOfMeasure, GetUnitName(p.UnitOfMeasure),
            p.SalePrice, p.EstimatedCost,
            p.TracksInventory, p.AllowsFractionalQuantity,
            p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);

    internal static string GetProductTypeName(ProductType type) => type switch
    {
        ProductType.Standard => "Estándar",
        ProductType.PreparedItem => "Preparado",
        ProductType.Combo => "Combo",
        ProductType.Service => "Servicio",
        ProductType.Ingredient => "Ingrediente",
        ProductType.Packaging => "Empaque",
        _ => type.ToString()
    };

    internal static string GetUnitName(Domain.Enums.UnitOfMeasure unit) => unit switch
    {
        Domain.Enums.UnitOfMeasure.Unit => "Unidad",
        Domain.Enums.UnitOfMeasure.Pound => "Libra",
        Domain.Enums.UnitOfMeasure.Kilogram => "Kilogramo",
        Domain.Enums.UnitOfMeasure.Portion => "Porción",
        Domain.Enums.UnitOfMeasure.Liter => "Litro",
        Domain.Enums.UnitOfMeasure.Gram => "Gramo",
        Domain.Enums.UnitOfMeasure.Ounce => "Onza",
        _ => unit.ToString()
    };
}

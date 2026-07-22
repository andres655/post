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
        var barcode = NormalizeBarcode(command.Barcode);
        if (barcode is not null)
        {
            var barcodeExists = await db.Products
                .AnyAsync(p => p.BusinessId == command.BusinessId
                            && p.Barcode == barcode, ct);

            if (barcodeExists)
                return Result.Failure<ProductDto>(
                    Error.Conflict("Product.DuplicateBarcode",
                        $"Ya existe un producto con el codigo de barras '{barcode}' en este negocio."));
        }

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

        var componentInputs = NormalizeComponentInputs(command.InventoryComponents);
        if (componentInputs.Count == 0 && command.InventorySourceProductId.HasValue && command.InventorySourceQuantity.HasValue)
        {
            componentInputs.Add(new ProductInventoryComponentInput(
                command.InventorySourceProductId.Value,
                command.InventorySourceQuantity.Value));
        }

        var inventorySources = new Dictionary<Guid, Product>();
        if (componentInputs.Count > 0)
        {
            var componentProductIds = componentInputs.Select(c => c.ProductId).Distinct().ToList();
            inventorySources = await db.Products
                .Where(p => p.BusinessId == command.BusinessId
                         && p.IsActive
                         && componentProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            if (inventorySources.Count != componentProductIds.Count)
                return Result.Failure<ProductDto>(
                    Error.BusinessRule("Product.InvalidInventoryComponent", "Uno o mas componentes de inventario no existen o estan inactivos."));

            if (inventorySources.Values.Any(p => !p.TracksInventory))
                return Result.Failure<ProductDto>(
                    Error.BusinessRule("Product.InventorySourceDoesNotTrackInventory", "Todos los componentes deben controlar inventario."));
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
            barcode);

        if (currentUser is not null)
            product.SetCreatedBy(currentUser);

        db.Products.Add(product);

        foreach (var component in componentInputs)
        {
            db.ProductComponents.Add(ProductComponent.Create(
                product.Id,
                component.ProductId,
                component.Quantity));
        }

        await db.SaveChangesAsync(ct);

        return Result.Success(MapToDto(product, categoryName, BuildComponentDtos(componentInputs, inventorySources)));
    }

    internal static ProductDto MapToDto(
        Product p,
        string? categoryName,
        IReadOnlyList<ProductInventoryComponentDto>? inventoryComponents = null)
    {
        var components = inventoryComponents ?? [];
        var firstComponent = components.Count == 1 ? components[0] : null;

        return new(p.Id, p.BusinessId, p.CategoryId, categoryName,
            p.Code, p.Barcode, p.Name, p.Description,
            p.ProductType, GetProductTypeName(p.ProductType),
            p.UnitOfMeasure, GetUnitName(p.UnitOfMeasure),
            p.SalePrice, p.EstimatedCost,
            p.TracksInventory, p.AllowsFractionalQuantity,
            firstComponent?.ProductId, firstComponent?.ProductName, firstComponent?.Quantity,
            components,
            p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);
    }

    internal static List<ProductInventoryComponentInput> NormalizeComponentInputs(IReadOnlyList<ProductInventoryComponentInput>? components) =>
        components?
            .Where(c => c.ProductId != Guid.Empty && c.Quantity > 0m)
            .GroupBy(c => c.ProductId)
            .Select(g => new ProductInventoryComponentInput(g.Key, g.Sum(c => c.Quantity)))
            .ToList()
        ?? [];

    internal static IReadOnlyList<ProductInventoryComponentDto> BuildComponentDtos(
        IReadOnlyList<ProductInventoryComponentInput> inputs,
        IReadOnlyDictionary<Guid, Product> products) =>
        inputs
            .Where(input => products.ContainsKey(input.ProductId))
            .Select(input =>
            {
                var product = products[input.ProductId];
                return new ProductInventoryComponentDto(product.Id, product.Code, product.Name, input.Quantity);
            })
            .ToList();

    internal static string? NormalizeBarcode(string? barcode) =>
        string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();

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

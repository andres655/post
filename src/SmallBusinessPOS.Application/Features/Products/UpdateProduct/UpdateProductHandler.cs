using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.Products.CreateProduct;
using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

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
        var barcode = CreateProduct.CreateProductHandler.NormalizeBarcode(command.Barcode);
        if (barcode is not null)
        {
            var barcodeExists = await db.Products
                .AnyAsync(p => p.BusinessId == product.BusinessId
                            && p.Barcode == barcode
                            && p.Id != command.Id, ct);

            if (barcodeExists)
                return Result.Failure<ProductDto>(
                    Error.Conflict("Product.DuplicateBarcode",
                        $"Ya existe otro producto con el codigo de barras '{barcode}' en este negocio."));
        }

        string? categoryName = null;
        if (command.CategoryId.HasValue)
        {
            var category = await db.Categories
                .FirstOrDefaultAsync(c => c.Id == command.CategoryId.Value && c.BusinessId == product.BusinessId, ct);

            if (category is null)
                return Result.Failure<ProductDto>(Error.NotFound("Category", command.CategoryId.Value));

            categoryName = category.Name;
        }

        var componentInputs = CreateProduct.CreateProductHandler.NormalizeComponentInputs(command.InventoryComponents);
        if (componentInputs.Count == 0 && command.InventorySourceProductId.HasValue && command.InventorySourceQuantity.HasValue)
        {
            componentInputs.Add(new ProductInventoryComponentInput(
                command.InventorySourceProductId.Value,
                command.InventorySourceQuantity.Value));
        }

        if (componentInputs.Any(c => c.ProductId == product.Id))
            return Result.Failure<ProductDto>(
                Error.BusinessRule("Product.InventorySourceCannotBeSelf", "Un producto no puede descontarse a si mismo."));

        var inventorySources = new Dictionary<Guid, Product>();
        if (componentInputs.Count > 0)
        {
            var componentProductIds = componentInputs.Select(c => c.ProductId).Distinct().ToList();
            inventorySources = await db.Products
                .Where(p => p.BusinessId == product.BusinessId
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

        product.Update(
            command.Code, command.Name, command.Description,
            command.ProductType, command.UnitOfMeasure,
            command.SalePrice, command.EstimatedCost,
            command.CategoryId, command.TracksInventory,
            command.AllowsFractionalQuantity, barcode);

        if (currentUser is not null)
            product.SetUpdated(currentUser);

        if (componentInputs.Count > 0)
        {
            var existingComponents = await db.ProductComponents
                .Where(c => c.ParentProductId == product.Id)
                .ToListAsync(ct);

            db.ProductComponents.RemoveRange(existingComponents);

            foreach (var component in componentInputs)
            {
                db.ProductComponents.Add(ProductComponent.Create(
                    product.Id,
                    component.ProductId,
                    component.Quantity));
            }
        }
        else if (command.ClearInventorySource)
        {
            var existingComponents = await db.ProductComponents
                .Where(c => c.ParentProductId == product.Id)
                .ToListAsync(ct);

            db.ProductComponents.RemoveRange(existingComponents);
        }

        await db.SaveChangesAsync(ct);

        return Result.Success(CreateProduct.CreateProductHandler.MapToDto(
            product,
            categoryName,
            CreateProduct.CreateProductHandler.BuildComponentDtos(componentInputs, inventorySources)));
    }
}

using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.Production.SaveProductionRecipe;

public sealed class SaveProductionRecipeHandler(
    IAppDbContext db,
    SaveProductionRecipeValidator validator)
{
    public async Task<Result> HandleAsync(
        SaveProductionRecipeCommand command,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var parent = await db.Products
            .FirstOrDefaultAsync(p => p.Id == command.ParentProductId, ct);

        if (parent is null)
            return Result.Failure(Error.NotFound("Product", command.ParentProductId));

        if (parent.BusinessId != command.BusinessId || !parent.IsActive)
            return Result.Failure(Error.BusinessRule("ProductionRecipe.InvalidProduct", "El producto producido no esta activo para este negocio."));

        if (!parent.TracksInventory)
            return Result.Failure(Error.BusinessRule("ProductionRecipe.ProductDoesNotTrackInventory", "El producto producido no controla inventario."));

        var duplicate = command.Components
            .GroupBy(c => c.ProductId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
            return Result.Failure(Error.BusinessRule("ProductionRecipe.DuplicateComponent", "La receta no puede repetir el mismo insumo."));

        if (command.Components.Any(c => c.ProductId == command.ParentProductId))
            return Result.Failure(Error.BusinessRule("ProductionRecipe.SelfReference", "Un producto no puede consumirse a si mismo como insumo."));

        var componentIds = command.Components.Select(c => c.ProductId).ToList();
        var products = await db.Products
            .Where(p => componentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var component in command.Components)
        {
            if (!products.TryGetValue(component.ProductId, out var product))
                return Result.Failure(Error.NotFound("Product", component.ProductId));

            if (product.BusinessId != command.BusinessId || !product.IsActive)
                return Result.Failure(Error.BusinessRule("ProductionRecipe.InvalidComponent", $"El insumo '{product.Code}' no esta activo para este negocio."));

            if (!product.TracksInventory)
                return Result.Failure(Error.BusinessRule("ProductionRecipe.ComponentDoesNotTrackInventory", $"El insumo '{product.Code}' no controla inventario."));
        }

        var existing = await db.ProductComponents
            .Where(c => c.ParentProductId == command.ParentProductId)
            .ToListAsync(ct);

        db.ProductComponents.RemoveRange(existing);

        foreach (var component in command.Components)
        {
            db.ProductComponents.Add(ProductComponent.Create(
                command.ParentProductId,
                component.ProductId,
                component.Quantity));
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

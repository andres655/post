using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Inventory.AdjustInventory;

public sealed class AdjustInventoryHandler(
    IAppDbContext db,
    AdjustInventoryValidator validator)
{
    public async Task<Result<AdjustInventoryResultDto>> HandleAsync(
        AdjustInventoryCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<AdjustInventoryResultDto>(
                Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.ProductId, ct);
        if (product is null)
            return Result.Failure<AdjustInventoryResultDto>(Error.NotFound("Product", command.ProductId));

        if (!product.TracksInventory)
            return Result.Failure<AdjustInventoryResultDto>(
                Error.BusinessRule("Inventory.ProductDoesNotTrackInventory", "El producto no controla inventario."));

        var settings = await db.BusinessSettings
            .FirstOrDefaultAsync(s => s.BusinessId == command.BusinessId, ct);

        var stock = await db.InventoryStocks
            .FirstOrDefaultAsync(s => s.BusinessId == command.BusinessId
                                   && s.BranchId == command.BranchId
                                   && s.ProductId == command.ProductId, ct);

        if (stock is null)
        {
            stock = InventoryStock.Create(command.BusinessId, command.BranchId, command.ProductId, 0m);
            db.InventoryStocks.Add(stock);
        }

        var previous = stock.Quantity;
        var next = previous + command.Quantity;

        if (next < 0m && settings?.AllowsNegativeInventory != true)
            return Result.Failure<AdjustInventoryResultDto>(
                Error.BusinessRule("Inventory.NegativeStockNotAllowed", "El ajuste dejaría el inventario en negativo."));

        stock.ApplyMovement(next);
        stock.SetUpdated(currentUser ?? "system");

        db.InventoryMovements.Add(InventoryMovement.Create(
            command.BusinessId,
            command.BranchId,
            command.ProductId,
            command.Quantity > 0m ? MovementType.AdjustmentIncrease : MovementType.AdjustmentDecrease,
            Math.Abs(command.Quantity),
            previous,
            next,
            referenceType: "ManualAdjustment",
            reason: command.Reason,
            createdBy: currentUser));

        await db.SaveChangesAsync(ct);

        return Result.Success(new AdjustInventoryResultDto(command.ProductId, previous, next));
    }
}

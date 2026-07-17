using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Features.Inventory.SetMinimumStock;

public sealed class SetMinimumStockHandler(
    IAppDbContext db,
    SetMinimumStockValidator validator)
{
    public async Task<Result> HandleAsync(
        SetMinimumStockCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.ProductId, ct);
        if (product is null)
            return Result.Failure(Error.NotFound("Product", command.ProductId));

        if (!product.TracksInventory)
            return Result.Failure(
                Error.BusinessRule("Inventory.ProductDoesNotTrackInventory", "El producto no controla inventario."));

        var stock = await db.InventoryStocks
            .FirstOrDefaultAsync(s => s.BusinessId == command.BusinessId
                                   && s.BranchId == command.BranchId
                                   && s.ProductId == command.ProductId, ct);

        if (stock is null)
        {
            stock = InventoryStock.Create(command.BusinessId, command.BranchId, command.ProductId, 0m);
            db.InventoryStocks.Add(stock);
        }

        stock.SetMinimumQuantity(command.MinimumQuantity);
        stock.SetUpdated(currentUser ?? "system");

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

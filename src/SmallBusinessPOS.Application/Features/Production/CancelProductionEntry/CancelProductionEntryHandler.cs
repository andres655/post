using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Production.CancelProductionEntry;

public sealed class CancelProductionEntryHandler(
    IAppDbContext db,
    CancelProductionEntryValidator validator)
{
    public async Task<Result> HandleAsync(
        CancelProductionEntryCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var entry = await db.ProductionEntries
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.Id == command.ProductionEntryId, ct);

        if (entry is null)
            return Result.Failure(Error.NotFound("ProductionEntry", command.ProductionEntryId));

        if (entry.Status != ProductionEntryStatus.Confirmed)
            return Result.Failure(Error.BusinessRule(
                "ProductionEntry.NotConfirmed",
                "Solo se puede anular una produccion confirmada."));

        var settings = await db.BusinessSettings
            .FirstOrDefaultAsync(s => s.BusinessId == entry.BusinessId, ct);

        foreach (var detail in entry.Details)
        {
            var netQuantity = detail.QuantityProduced - detail.QuantityWasted;
            if (netQuantity <= 0m)
                continue;

            var stock = await db.InventoryStocks
                .FirstOrDefaultAsync(s => s.BusinessId == entry.BusinessId
                                       && s.BranchId == entry.BranchId
                                       && s.ProductId == detail.ProductId, ct);

            if (stock is null)
                return Result.Failure(Error.BusinessRule(
                    "ProductionEntry.StockNotFound",
                    "No existe inventario para revertir la produccion."));

            var next = stock.Quantity - netQuantity;
            if (next < 0m && settings?.AllowsNegativeInventory != true)
                return Result.Failure(Error.BusinessRule(
                    "ProductionEntry.InsufficientStock",
                    "No hay existencia suficiente para revertir la produccion."));
        }

        foreach (var detail in entry.Details)
        {
            var netQuantity = detail.QuantityProduced - detail.QuantityWasted;
            if (netQuantity <= 0m)
                continue;

            var stock = await db.InventoryStocks
                .FirstAsync(s => s.BusinessId == entry.BusinessId
                              && s.BranchId == entry.BranchId
                              && s.ProductId == detail.ProductId, ct);

            var previous = stock.Quantity;
            var next = previous - netQuantity;
            stock.ApplyMovement(next);
            stock.SetUpdated(currentUser ?? "system");

            db.InventoryMovements.Add(InventoryMovement.Create(
                entry.BusinessId,
                entry.BranchId,
                detail.ProductId,
                MovementType.ProductionCancellation,
                netQuantity,
                previous,
                next,
                referenceType: "ProductionEntryCancellation",
                referenceId: entry.Id,
                reason: $"Anulacion de produccion {entry.Number}: {command.Reason}",
                createdBy: currentUser));
        }

        var inputMovements = await db.InventoryMovements
            .Where(m => m.ReferenceType == "ProductionEntry"
                     && m.ReferenceId == entry.Id
                     && m.MovementType == MovementType.ProductionInput)
            .ToListAsync(ct);

        foreach (var input in inputMovements
                     .GroupBy(m => m.ProductId)
                     .Select(g => new { ProductId = g.Key, Quantity = g.Sum(m => m.Quantity) }))
        {
            var stock = await db.InventoryStocks
                .FirstOrDefaultAsync(s => s.BusinessId == entry.BusinessId
                                       && s.BranchId == entry.BranchId
                                       && s.ProductId == input.ProductId, ct);

            if (stock is null)
            {
                stock = InventoryStock.Create(entry.BusinessId, entry.BranchId, input.ProductId, 0m);
                db.InventoryStocks.Add(stock);
            }

            var previous = stock.Quantity;
            var next = previous + input.Quantity;
            stock.ApplyMovement(next);
            stock.SetUpdated(currentUser ?? "system");

            db.InventoryMovements.Add(InventoryMovement.Create(
                entry.BusinessId,
                entry.BranchId,
                input.ProductId,
                MovementType.ProductionCancellation,
                input.Quantity,
                previous,
                next,
                referenceType: "ProductionEntryCancellation",
                referenceId: entry.Id,
                reason: $"Reintegro de insumo por anulacion {entry.Number}: {command.Reason}",
                createdBy: currentUser));
        }

        entry.Cancel(currentUser);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

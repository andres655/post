using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;

public sealed class ConfirmProductionEntryHandler(
    IAppDbContext db,
    ConfirmProductionEntryValidator validator)
{
    public async Task<Result<ConfirmProductionEntryResultDto>> HandleAsync(
        ConfirmProductionEntryCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<ConfirmProductionEntryResultDto>(
                Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var entry = command.ProductionEntryId is null
            ? await CreateDraftEntryAsync(command, currentUser, ct)
            : await db.ProductionEntries
                .Include(e => e.Details)
                .FirstOrDefaultAsync(e => e.Id == command.ProductionEntryId.Value, ct);

        if (entry is null)
            return Result.Failure<ConfirmProductionEntryResultDto>(
                Error.NotFound("ProductionEntry", command.ProductionEntryId!.Value));

        if (entry.BusinessId != command.BusinessId || entry.BranchId != command.BranchId)
            return Result.Failure<ConfirmProductionEntryResultDto>(
                Error.BusinessRule("ProductionEntry.ScopeMismatch", "La produccion no pertenece al negocio o sucursal indicada."));

        if (entry.Status == ProductionEntryStatus.Confirmed)
            return Result.Failure<ConfirmProductionEntryResultDto>(
                Error.BusinessRule("ProductionEntry.AlreadyConfirmed", "La produccion ya fue confirmada."));

        var inputLines = command.Inputs is { Count: > 0 }
            ? command.Inputs
            : await BuildRecipeInputLinesAsync(entry, ct);

        var inputQuantities = inputLines
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var productIds = entry.Details.Select(d => d.ProductId)
            .Concat(inputQuantities.Keys)
            .Distinct()
            .ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var detail in entry.Details)
        {
            if (!products.TryGetValue(detail.ProductId, out var product))
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.NotFound("Product", detail.ProductId));

            if (product.BusinessId != command.BusinessId || !product.IsActive)
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.BusinessRule("ProductionEntry.InvalidProduct", $"El producto '{product.Code}' no esta activo para este negocio."));

            if (!product.TracksInventory)
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.BusinessRule("ProductionEntry.ProductDoesNotTrackInventory", $"El producto '{product.Code}' no controla inventario."));
        }

        foreach (var input in inputQuantities)
        {
            if (!products.TryGetValue(input.Key, out var product))
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.NotFound("Product", input.Key));

            if (product.BusinessId != command.BusinessId || !product.IsActive)
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.BusinessRule("ProductionEntry.InvalidInputProduct", $"El insumo '{product.Code}' no esta activo para este negocio."));

            if (!product.TracksInventory)
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.BusinessRule("ProductionEntry.InputDoesNotTrackInventory", $"El insumo '{product.Code}' no controla inventario."));
        }

        var settings = await db.BusinessSettings
            .FirstOrDefaultAsync(s => s.BusinessId == command.BusinessId, ct);

        foreach (var input in inputQuantities)
        {
            var stock = await db.InventoryStocks
                .FirstOrDefaultAsync(s => s.BusinessId == command.BusinessId
                                       && s.BranchId == command.BranchId
                                       && s.ProductId == input.Key, ct);

            if (stock is null)
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.BusinessRule("ProductionEntry.InputStockRequired", "No existe inventario para uno de los insumos."));

            var next = stock.Quantity - input.Value;
            if (next < 0m && settings?.AllowsNegativeInventory != true)
                return Result.Failure<ConfirmProductionEntryResultDto>(
                    Error.BusinessRule("ProductionEntry.InsufficientInputStock", $"No hay existencia suficiente del insumo '{products[input.Key].Code}'."));
        }

        foreach (var input in inputQuantities)
        {
            var stock = await db.InventoryStocks
                .FirstAsync(s => s.BusinessId == command.BusinessId
                              && s.BranchId == command.BranchId
                              && s.ProductId == input.Key, ct);

            var previous = stock.Quantity;
            var next = previous - input.Value;
            stock.ApplyMovement(next);

            db.InventoryMovements.Add(InventoryMovement.Create(
                command.BusinessId,
                command.BranchId,
                input.Key,
                MovementType.ProductionInput,
                input.Value,
                previous,
                next,
                referenceType: "ProductionEntry",
                referenceId: entry.Id,
                reason: $"Insumo consumido en produccion {entry.Number}",
                createdBy: currentUser));
        }

        foreach (var detail in entry.Details)
        {
            var stock = await db.InventoryStocks
                .FirstOrDefaultAsync(s => s.BusinessId == command.BusinessId
                                       && s.BranchId == command.BranchId
                                       && s.ProductId == detail.ProductId, ct);

            if (stock is null)
            {
                stock = InventoryStock.Create(command.BusinessId, command.BranchId, detail.ProductId, 0m);
                db.InventoryStocks.Add(stock);
            }

            var previous = stock.Quantity;
            var afterProduction = previous + detail.QuantityProduced;
            stock.ApplyMovement(afterProduction);

            db.InventoryMovements.Add(InventoryMovement.Create(
                command.BusinessId,
                command.BranchId,
                detail.ProductId,
                MovementType.ProductionOutput,
                detail.QuantityProduced,
                previous,
                afterProduction,
                referenceType: "ProductionEntry",
                referenceId: entry.Id,
                reason: $"Produccion {entry.Number}",
                createdBy: currentUser));

            if (detail.QuantityWasted > 0)
            {
                var afterWaste = afterProduction - detail.QuantityWasted;
                stock.ApplyMovement(afterWaste);

                db.InventoryMovements.Add(InventoryMovement.Create(
                    command.BusinessId,
                    command.BranchId,
                    detail.ProductId,
                    MovementType.Waste,
                    detail.QuantityWasted,
                    afterProduction,
                    afterWaste,
                    referenceType: "ProductionEntry",
                    referenceId: entry.Id,
                    reason: $"Merma de produccion {entry.Number}",
                    createdBy: currentUser));
            }
        }

        entry.Confirm(currentUser);
        await db.SaveChangesAsync(ct);

        return Result.Success(new ConfirmProductionEntryResultDto(
            entry.Id,
            entry.Number,
            entry.ProductionDate,
            entry.Details.Count,
            entry.Details.Sum(d => d.QuantityProduced),
            entry.Details.Sum(d => d.QuantityWasted),
            entry.Details.Sum(d => d.QuantityProduced - d.QuantityWasted),
            inputQuantities.Values.Sum()));
    }

    private async Task<ProductionEntry> CreateDraftEntryAsync(
        ConfirmProductionEntryCommand command,
        string? currentUser,
        CancellationToken ct)
    {
        var number = string.IsNullOrWhiteSpace(command.Number)
            ? await GenerateNumberAsync(command.BusinessId, command.BranchId, command.ProductionDate, ct)
            : command.Number;

        var entry = ProductionEntry.Create(
            command.BusinessId,
            command.BranchId,
            number!,
            command.ProductionDate,
            command.Notes,
            currentUser);

        foreach (var line in command.Lines)
        {
            entry.AddDetail(ProductionEntryDetail.Create(
                entry.Id,
                line.ProductId,
                line.QuantityProduced,
                line.UnitCost,
                line.QuantityWasted));
        }

        db.ProductionEntries.Add(entry);
        return entry;
    }

    private async Task<IReadOnlyList<ConfirmProductionInputLine>> BuildRecipeInputLinesAsync(
        ProductionEntry entry,
        CancellationToken ct)
    {
        var producedProductIds = entry.Details.Select(d => d.ProductId).Distinct().ToList();
        var components = await db.ProductComponents
            .Where(c => producedProductIds.Contains(c.ParentProductId))
            .ToListAsync(ct);

        if (components.Count == 0)
            return [];

        return entry.Details
            .SelectMany(detail => components
                .Where(component => component.ParentProductId == detail.ProductId)
                .Select(component => new ConfirmProductionInputLine(
                    component.ComponentProductId,
                    component.Quantity * detail.QuantityProduced)))
            .GroupBy(input => input.ProductId)
            .Select(group => new ConfirmProductionInputLine(
                group.Key,
                group.Sum(input => input.Quantity)))
            .ToList();
    }

    private async Task<string> GenerateNumberAsync(
        Guid businessId,
        Guid branchId,
        DateOnly productionDate,
        CancellationToken ct)
    {
        var prefix = $"PROD-{productionDate:yyyyMMdd}";
        var count = await db.ProductionEntries
            .Where(e => e.BusinessId == businessId
                     && e.BranchId == branchId
                     && e.ProductionDate == productionDate)
            .CountAsync(ct);

        return $"{prefix}-{count + 1:0000}";
    }
}

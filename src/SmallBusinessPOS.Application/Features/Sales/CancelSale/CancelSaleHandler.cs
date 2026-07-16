using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.CancelSale;

public sealed class CancelSaleHandler(
    IAppDbContext db,
    CancelSaleValidator validator)
{
    public async Task<Result> HandleAsync(
        CancelSaleCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var sale = await db.Sales
            .Include(s => s.CashSession)
            .FirstOrDefaultAsync(s => s.Id == command.SaleId, ct);

        if (sale is null)
            return Result.Failure(Error.NotFound("Sale", command.SaleId));

        if (sale.Status != SaleStatus.Confirmed)
            return Result.Failure(Error.BusinessRule("Sale.NotConfirmed", "Solo se pueden cancelar ventas confirmadas."));

        var saleMovements = await db.InventoryMovements
            .Where(m => m.ReferenceType == "Sale"
                     && m.ReferenceId == sale.Id
                     && m.MovementType == MovementType.Sale)
            .ToListAsync(ct);

        if (saleMovements.Count > 0)
        {
            var stockByProduct = await db.InventoryStocks
                .Where(s => s.BusinessId == sale.BusinessId
                         && s.BranchId == sale.BranchId
                         && saleMovements.Select(m => m.ProductId).Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId, ct);

            foreach (var movement in saleMovements)
            {
                if (!stockByProduct.TryGetValue(movement.ProductId, out var stock))
                {
                    stock = InventoryStock.Create(sale.BusinessId, sale.BranchId, movement.ProductId, 0m);
                    db.InventoryStocks.Add(stock);
                    stockByProduct[movement.ProductId] = stock;
                }

                var prev = stock.Quantity;
                var next = prev + movement.Quantity;

                stock.ApplyMovement(next);

                db.InventoryMovements.Add(InventoryMovement.Create(
                    sale.BusinessId,
                    sale.BranchId,
                    movement.ProductId,
                    MovementType.SaleCancellation,
                    movement.Quantity,
                    prev,
                    next,
                    referenceType: "SaleCancellation",
                    referenceId: sale.Id,
                    reason: command.Reason,
                    deviceId: command.DeviceId,
                    createdBy: currentUser));
            }
        }

        var cashMovements = await db.CashMovements
            .Where(m => m.ReferenceType == "Sale"
                     && m.ReferenceId == sale.Id
                     && m.MovementType == CashMovementType.SaleIncome)
            .ToListAsync(ct);

        if (cashMovements.Count > 0)
        {
            if (sale.CashSession is null)
                return Result.Failure(Error.BusinessRule("Sale.CashSessionMissing", "La venta no tiene sesión de caja asociada."));

            if (sale.CashSession.Status != CashSessionStatus.Open)
                return Result.Failure(Error.BusinessRule("Sale.CashSessionClosed", "La caja está cerrada. No se puede cancelar sin reapertura."));

            foreach (var movement in cashMovements)
            {
                sale.CashSession.AddExpense(movement.Amount);

                db.CashMovements.Add(CashMovement.Create(
                    sale.BusinessId,
                    sale.BranchId,
                    sale.CashSession.Id,
                    CashMovementType.Refund,
                    movement.Amount,
                    $"Reverso por cancelación de venta {sale.ReceiptNumber}",
                    referenceType: "SaleCancellation",
                    referenceId: sale.Id,
                    paymentMethodId: movement.PaymentMethodId,
                    createdBy: currentUser));
            }
        }

        sale.Cancel(command.Reason, currentUser);

        var payload = JsonSerializer.Serialize(new
        {
            SaleId = sale.Id,
            sale.ReceiptNumber,
            Reason = command.Reason,
            CancelledAtUtc = DateTime.UtcNow
        });

        db.OutboxMessages.Add(OutboxMessage.Create(
            sale.BusinessId,
            eventType: "SaleCancelled",
            aggregateId: sale.Id,
            payload: payload));

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

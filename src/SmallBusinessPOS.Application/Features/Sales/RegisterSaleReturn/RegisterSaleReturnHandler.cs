using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.RegisterSaleReturn;

public sealed class RegisterSaleReturnHandler(
    IAppDbContext db,
    RegisterSaleReturnValidator validator)
{
    public async Task<Result<Guid>> HandleAsync(
        RegisterSaleReturnCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<Guid>(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var sale = await db.Sales
            .Include(item => item.Details)
            .Include(item => item.CashSession)
            .FirstOrDefaultAsync(item => item.Id == command.SaleId, ct);
        if (sale is null)
            return Result.Failure<Guid>(Error.NotFound("Sale", command.SaleId));

        if (sale.Status is not SaleStatus.Confirmed and not SaleStatus.PartiallyRefunded)
            return Result.Failure<Guid>(Error.BusinessRule("Sale.NotReturnable", "La venta no admite devoluciones."));

        var refundMethod = await db.PaymentMethods
            .FirstOrDefaultAsync(method => method.Id == command.RefundPaymentMethodId
                                        && method.BusinessId == sale.BusinessId
                                        && method.IsActive, ct);
        if (refundMethod is null)
            return Result.Failure<Guid>(Error.NotFound("PaymentMethod", command.RefundPaymentMethodId));

        if (refundMethod.Type != PaymentMethodType.Cash && string.IsNullOrWhiteSpace(command.RefundReference))
        {
            return Result.Failure<Guid>(
                Error.BusinessRule("SaleReturn.ReferenceRequired", $"Debe indicar una referencia para reembolso por {refundMethod.Name}."));
        }

        if (refundMethod.Type == PaymentMethodType.Cash)
        {
            if (sale.CashSession is null || sale.CashSession.Status != CashSessionStatus.Open)
                return Result.Failure<Guid>(Error.BusinessRule("SaleReturn.OpenCashRequired", "La caja debe estar abierta para devolver efectivo."));
        }

        var saleDetails = sale.Details.ToDictionary(detail => detail.Id);
        var detailIds = saleDetails.Keys.ToList();
        var returnedByDetail = await db.SaleReturnDetails
            .Where(detail => detailIds.Contains(detail.SaleDetailId))
            .GroupBy(detail => detail.SaleDetailId)
            .Select(group => new { SaleDetailId = group.Key, Quantity = group.Sum(detail => detail.Quantity) })
            .ToDictionaryAsync(item => item.SaleDetailId, item => item.Quantity, ct);

        foreach (var line in command.Lines)
        {
            if (!saleDetails.TryGetValue(line.SaleDetailId, out var detail))
                return Result.Failure<Guid>(Error.NotFound("SaleDetail", line.SaleDetailId));

            var returned = returnedByDetail.GetValueOrDefault(line.SaleDetailId);
            if (line.Quantity > detail.Quantity - returned)
            {
                return Result.Failure<Guid>(
                    Error.BusinessRule("SaleReturn.QuantityExceeded", $"La cantidad devuelta supera lo disponible para {detail.ProductName}."));
            }
        }

        var returnCount = await db.SaleReturns.CountAsync(item => item.SaleId == sale.Id, ct);
        var saleReturn = SaleReturn.Create(
            sale.BusinessId,
            sale.BranchId,
            sale.Id,
            $"{sale.ReceiptNumber}-D{returnCount + 1:00}",
            command.Reason,
            refundMethod.Type == PaymentMethodType.Cash ? sale.CashSessionId : null,
            command.RefundReference,
            currentUser);

        foreach (var line in command.Lines)
        {
            var detail = saleDetails[line.SaleDetailId];
            saleReturn.AddDetail(SaleReturnDetail.Create(
                saleReturn.Id,
                detail.Id,
                detail.ProductId,
                detail.ProductCode,
                detail.ProductName,
                line.Quantity,
                detail.UnitPrice));
        }

        var inventoryReturn = await BuildInventoryReturnAsync(saleReturn, ct);
        if (inventoryReturn.Count > 0)
        {
            var productIds = inventoryReturn.Keys.ToList();
            var stocks = await db.InventoryStocks
                .Where(stock => stock.BusinessId == sale.BusinessId
                             && stock.BranchId == sale.BranchId
                             && productIds.Contains(stock.ProductId))
                .ToDictionaryAsync(stock => stock.ProductId, ct);

            foreach (var item in inventoryReturn)
            {
                if (!stocks.TryGetValue(item.Key, out var stock))
                {
                    stock = InventoryStock.Create(sale.BusinessId, sale.BranchId, item.Key, 0m);
                    db.InventoryStocks.Add(stock);
                    stocks[item.Key] = stock;
                }

                var previous = stock.Quantity;
                var next = previous + item.Value;
                stock.ApplyMovement(next);

                db.InventoryMovements.Add(InventoryMovement.Create(
                    sale.BusinessId,
                    sale.BranchId,
                    item.Key,
                    MovementType.Return,
                    item.Value,
                    previous,
                    next,
                    referenceType: "SaleReturn",
                    referenceId: saleReturn.Id,
                    reason: command.Reason,
                    deviceId: command.DeviceId,
                    createdBy: currentUser));
            }
        }

        if (refundMethod.Type == PaymentMethodType.Cash && sale.CashSession is not null)
        {
            sale.CashSession.AddExpense(saleReturn.Total);
            db.CashMovements.Add(CashMovement.Create(
                sale.BusinessId,
                sale.BranchId,
                sale.CashSession.Id,
                CashMovementType.Refund,
                saleReturn.Total,
                $"Devolucion parcial venta {sale.ReceiptNumber}",
                referenceType: "SaleReturn",
                referenceId: saleReturn.Id,
                paymentMethodId: refundMethod.Id,
                createdBy: currentUser));
        }

        db.SaleReturns.Add(saleReturn);

        var totalReturned = await db.SaleReturnDetails
            .Where(detail => detail.SaleReturn.SaleId == sale.Id)
            .SumAsync(detail => (decimal?)detail.Quantity * detail.UnitPrice, ct) ?? 0m;
        sale.ApplyReturnStatus(totalReturned + saleReturn.Total, currentUser);

        db.OutboxMessages.Add(OutboxMessage.Create(
            sale.BusinessId,
            "SaleReturned",
            saleReturn.Id,
            JsonSerializer.Serialize(new
            {
                SaleReturnId = saleReturn.Id,
                saleReturn.ReturnNumber,
                saleReturn.SaleId,
                saleReturn.Total,
                saleReturn.ReturnedAtUtc
            })));

        await db.SaveChangesAsync(ct);
        return Result.Success(saleReturn.Id);
    }

    private async Task<Dictionary<Guid, decimal>> BuildInventoryReturnAsync(SaleReturn saleReturn, CancellationToken ct)
    {
        var productIds = saleReturn.Details.Select(detail => detail.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, ct);

        var components = await db.ProductComponents
            .Include(component => component.ComponentProduct)
            .Where(component => productIds.Contains(component.ParentProductId))
            .ToListAsync(ct);

        var componentsByParent = components
            .GroupBy(component => component.ParentProductId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var result = new Dictionary<Guid, decimal>();
        foreach (var detail in saleReturn.Details)
        {
            var product = products[detail.ProductId];
            if (componentsByParent.TryGetValue(product.Id, out var productComponents) && productComponents.Count > 0)
            {
                foreach (var component in productComponents)
                {
                    if (!component.ComponentProduct.TracksInventory)
                        continue;

                    AddQuantity(result, component.ComponentProductId, detail.Quantity * component.Quantity);
                }
            }
            else if (product.TracksInventory)
            {
                AddQuantity(result, product.Id, detail.Quantity);
            }
        }

        return result;
    }

    private static void AddQuantity(Dictionary<Guid, decimal> values, Guid productId, decimal quantity)
    {
        if (values.TryGetValue(productId, out var existing))
            values[productId] = existing + quantity;
        else
            values[productId] = quantity;
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Sales.CreateSale;

public sealed class CreateSaleHandler(
    IAppDbContext db,
    CreateSaleValidator validator,
    IClock clock)
{
    public async Task<Result<CreateSaleResultDto>> HandleAsync(
        CreateSaleCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<CreateSaleResultDto>(
                Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var register = await db.CashRegisters
            .FirstOrDefaultAsync(r => r.Id == command.CashRegisterId
                                   && r.BusinessId == command.BusinessId
                                   && r.BranchId == command.BranchId
                                   && r.IsActive, ct);

        if (register is null)
            return Result.Failure<CreateSaleResultDto>(
                Error.NotFound("CashRegister", command.CashRegisterId));

        var session = await db.CashSessions
            .FirstOrDefaultAsync(s => s.CashRegisterId == command.CashRegisterId && s.Status == CashSessionStatus.Open, ct);

        if (session is null)
            return Result.Failure<CreateSaleResultDto>(
                Error.BusinessRule("Sale.CashSessionRequired", "No se puede vender sin una caja abierta."));

        var paymentMethodIds = command.Payments.Select(p => p.PaymentMethodId).Distinct().ToList();
        var paymentMethods = await db.PaymentMethods
            .Where(pm => pm.BusinessId == command.BusinessId && pm.IsActive && paymentMethodIds.Contains(pm.Id))
            .ToDictionaryAsync(pm => pm.Id, ct);

        if (paymentMethods.Count != paymentMethodIds.Count)
            return Result.Failure<CreateSaleResultDto>(
                Error.BusinessRule("Sale.InvalidPaymentMethod", "Uno o más métodos de pago no están activos."));

        var productIds = command.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => p.BusinessId == command.BusinessId && p.IsActive && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count != productIds.Count)
            return Result.Failure<CreateSaleResultDto>(
                Error.BusinessRule("Sale.InvalidProduct", "Uno o más productos no existen o están inactivos."));

        var businessSettings = await db.BusinessSettings
            .FirstOrDefaultAsync(s => s.BusinessId == command.BusinessId, ct);

        var allowsNegativeInventory = businessSettings?.AllowsNegativeInventory ?? false;
        var allowsCredit = businessSettings?.AllowsCredit ?? false;
        var usesTaxes = businessSettings?.UsesTaxes ?? false;
        var defaultTaxRate = businessSettings?.DefaultTaxRate ?? 0m;

        Customer? customer = null;
        if (command.CustomerId.HasValue)
        {
            customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Id == command.CustomerId.Value
                                       && c.BusinessId == command.BusinessId
                                       && c.IsActive, ct);

            if (customer is null)
                return Result.Failure<CreateSaleResultDto>(Error.NotFound("Customer", command.CustomerId.Value));
        }

        foreach (var payment in command.Payments)
        {
            var method = paymentMethods[payment.PaymentMethodId];
            if (method.Type != PaymentMethodType.Cash
                && method.Type != PaymentMethodType.Credit
                && string.IsNullOrWhiteSpace(payment.Reference))
            {
                return Result.Failure<CreateSaleResultDto>(
                    Error.BusinessRule("Sale.PaymentReferenceRequired", $"Debe indicar una referencia para {method.Name}."));
            }

            if (method.Type == PaymentMethodType.Credit)
            {
                if (!allowsCredit)
                    return Result.Failure<CreateSaleResultDto>(
                        Error.BusinessRule("Sale.CreditNotAllowed", "El negocio no tiene ventas a credito habilitadas."));

                if (customer is null)
                    return Result.Failure<CreateSaleResultDto>(
                        Error.BusinessRule("Sale.CustomerRequiredForCredit", "Debe seleccionar un cliente para vender a credito."));
            }

            if (method.Type != PaymentMethodType.Cash
                && payment.TenderedAmount.HasValue
                && payment.TenderedAmount.Value != payment.Amount)
            {
                return Result.Failure<CreateSaleResultDto>(
                    Error.BusinessRule("Sale.ChangeOnlyForCash", "El cambio solo se puede calcular para pagos en efectivo."));
            }
        }

        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == command.BranchId, ct);
        if (branch is null)
            return Result.Failure<CreateSaleResultDto>(Error.NotFound("Branch", command.BranchId));

        var number = await GenerateSaleNumberAsync(
            command.BusinessId,
            command.BranchId,
            command.CashRegisterId,
            branch,
            register,
            ct);

        var sale = Sale.Create(
            command.BusinessId,
            command.BranchId,
            number,
            command.SaleType,
            session.Id,
            command.CustomerId,
            customer?.Name ?? command.CustomerName,
            command.Notes,
            currentUser);

        foreach (var line in command.Lines)
        {
            var product = products[line.ProductId];

            if (!product.AllowsFractionalQuantity && line.Quantity != decimal.Truncate(line.Quantity))
            {
                return Result.Failure<CreateSaleResultDto>(
                    Error.BusinessRule("Sale.FractionalQuantityNotAllowed", $"El producto {product.Name} no permite cantidades fraccionarias."));
            }

            sale.AddDetail(SaleDetail.Create(
                sale.Id,
                product.Id,
                product.Code,
                product.Name,
                line.Quantity,
                product.SalePrice));
        }

        if (command.Discount > sale.SubTotal)
        {
            return Result.Failure<CreateSaleResultDto>(
                Error.BusinessRule("Sale.InvalidDiscount", "El descuento no puede ser mayor al subtotal."));
        }

        var discount = Math.Round(command.Discount, 2, MidpointRounding.AwayFromZero);
        var tax = CalculateTax(sale.SubTotal, discount, usesTaxes, defaultTaxRate);
        sale.ApplyFinancials(discount, tax);

        var paidTotal = command.Payments.Sum(p => p.Amount);
        if (paidTotal != sale.Total)
        {
            return Result.Failure<CreateSaleResultDto>(
                Error.BusinessRule("Sale.PaymentMismatch", "El total de pagos debe coincidir con el total de la venta."));
        }

        foreach (var payment in command.Payments)
        {
            sale.AddPayment(SalePayment.Create(
                sale.Id,
                payment.PaymentMethodId,
                payment.Amount,
                payment.TenderedAmount,
                payment.Reference));
        }

        // Descuento automático de inventario (producto directo o componentes)
        var inventoryConsumption = await BuildInventoryConsumptionAsync(command, products, ct);

        if (inventoryConsumption.Count > 0)
        {
            var consumedProductIds = inventoryConsumption.Keys.ToList();

            var stocks = await db.InventoryStocks
                .Where(s => s.BusinessId == command.BusinessId
                         && s.BranchId == command.BranchId
                         && consumedProductIds.Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId, ct);

            foreach (var consumed in inventoryConsumption)
            {
                var productId = consumed.Key;
                var consumeQty = consumed.Value;

                if (!stocks.TryGetValue(productId, out var stock))
                {
                    stock = InventoryStock.Create(command.BusinessId, command.BranchId, productId, 0m);
                    db.InventoryStocks.Add(stock);
                    stocks[productId] = stock;
                }

                var prev = stock.Quantity;
                var next = prev - consumeQty;

                if (!allowsNegativeInventory && next < 0)
                {
                    return Result.Failure<CreateSaleResultDto>(
                        Error.BusinessRule("Sale.InsufficientInventory", "Inventario insuficiente para confirmar la venta."));
                }

                stock.ApplyMovement(next);

                db.InventoryMovements.Add(InventoryMovement.Create(
                    command.BusinessId,
                    command.BranchId,
                    productId,
                    MovementType.Sale,
                    consumeQty,
                    prev,
                    next,
                    referenceType: "Sale",
                    referenceId: sale.Id,
                    reason: "Salida por venta POS",
                    deviceId: command.DeviceId,
                    createdBy: currentUser));
            }
        }

        // Caja: solo efectivo afecta el efectivo esperado
        var cashIncome = command.Payments
            .Where(p => paymentMethods[p.PaymentMethodId].Type == PaymentMethodType.Cash)
            .Sum(p => p.Amount);

        if (cashIncome > 0)
        {
            session.AddIncome(cashIncome);

            db.CashMovements.Add(CashMovement.Create(
                command.BusinessId,
                command.BranchId,
                session.Id,
                CashMovementType.SaleIncome,
                cashIncome,
                "Ingreso por venta",
                referenceType: "Sale",
                referenceId: sale.Id,
                createdBy: currentUser));
        }

        sale.Confirm(currentUser);

        db.Sales.Add(sale);

        var payload = JsonSerializer.Serialize(new
        {
            SaleId = sale.Id,
            sale.ReceiptNumber,
            sale.BusinessId,
            sale.BranchId,
            sale.Total,
            sale.SoldAtUtc
        });

        db.OutboxMessages.Add(OutboxMessage.Create(
            command.BusinessId,
            eventType: "SaleConfirmed",
            aggregateId: sale.Id,
            payload: payload));

        await db.SaveChangesAsync(ct);

        var cashTendered = command.Payments
            .Where(p => paymentMethods[p.PaymentMethodId].Type == PaymentMethodType.Cash)
            .Sum(p => p.TenderedAmount ?? p.Amount);
        var cashApplied = command.Payments
            .Where(p => paymentMethods[p.PaymentMethodId].Type == PaymentMethodType.Cash)
            .Sum(p => p.Amount);
        var change = Math.Max(0m, cashTendered - cashApplied);

        return Result.Success(new CreateSaleResultDto(
            sale.Id,
            sale.ReceiptNumber,
            sale.SubTotal,
            sale.Discount,
            sale.Tax,
            sale.Total,
            paidTotal,
            change,
            sale.SoldAtUtc));
    }

    private async Task<Dictionary<Guid, decimal>> BuildInventoryConsumptionAsync(
        CreateSaleCommand command,
        IReadOnlyDictionary<Guid, Product> products,
        CancellationToken ct)
    {
        var parentProductIds = command.Lines.Select(l => l.ProductId).Distinct().ToList();

        var components = await db.ProductComponents
            .Include(c => c.ComponentProduct)
            .Where(c => parentProductIds.Contains(c.ParentProductId))
            .ToListAsync(ct);

        var componentsByParent = components
            .GroupBy(c => c.ParentProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new Dictionary<Guid, decimal>();

        foreach (var line in command.Lines)
        {
            var product = products[line.ProductId];

            if (componentsByParent.TryGetValue(product.Id, out var productComponents) && productComponents.Count > 0)
            {
                foreach (var component in productComponents)
                {
                    if (!component.ComponentProduct.TracksInventory)
                        continue;

                    var qty = line.Quantity * component.Quantity;
                    if (result.TryGetValue(component.ComponentProductId, out var existing))
                        result[component.ComponentProductId] = existing + qty;
                    else
                        result[component.ComponentProductId] = qty;
                }
            }
            else if (product.TracksInventory)
            {
                if (result.TryGetValue(product.Id, out var existing))
                    result[product.Id] = existing + line.Quantity;
                else
                    result[product.Id] = line.Quantity;
            }
        }

        return result;
    }

    private static decimal CalculateTax(decimal subtotal, decimal discount, bool usesTaxes, decimal defaultTaxRate)
    {
        if (!usesTaxes || defaultTaxRate <= 0)
            return 0m;

        var taxableBase = Math.Max(0m, subtotal - Math.Max(0m, discount));
        return Math.Round(taxableBase * (defaultTaxRate / 100m), 2, MidpointRounding.AwayFromZero);
    }

    private async Task<string> GenerateSaleNumberAsync(
        Guid businessId,
        Guid branchId,
        Guid cashRegisterId,
        Branch branch,
        CashRegister register,
        CancellationToken ct)
    {
        var date = clock.TodayUtc;

        var sequence = await db.SaleNumberSequences
            .FirstOrDefaultAsync(s => s.CashRegisterId == cashRegisterId && s.BusinessDate == date, ct);

        if (sequence is null)
        {
            sequence = SaleNumberSequence.Create(businessId, branchId, cashRegisterId, date);
            db.SaleNumberSequences.Add(sequence);
        }

        var next = sequence.IncrementAndGet();

        var branchCode = BuildBranchCode(branch);
        var datePart = date.ToString("yyyyMMdd");

        return $"{branchCode}-{register.Code}-{datePart}-{next:000000}";
    }

    private static string BuildBranchCode(Branch branch)
    {
        if (branch.IsMain)
            return "PRIN";

        var cleaned = new string(branch.Name
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(cleaned))
            return "BRCH";

        return cleaned.Length >= 4 ? cleaned[..4] : cleaned.PadRight(4, 'X');
    }
}

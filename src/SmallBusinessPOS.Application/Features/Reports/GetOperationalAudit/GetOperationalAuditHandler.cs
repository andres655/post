using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Reports.GetOperationalAudit;

public sealed class GetOperationalAuditHandler(IAppDbContext db)
{
    public async Task<Result<IReadOnlyList<OperationalAuditEntryDto>>> HandleAsync(
        GetOperationalAuditQuery query,
        CancellationToken ct = default)
    {
        if (query.To < query.From)
            return Result.Failure<IReadOnlyList<OperationalAuditEntryDto>>(
                Error.Validation("To", "La fecha final no puede ser menor que la fecha inicial."));

        var fromUtc = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = query.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var entries = new List<OperationalAuditEntryDto>();

        var sales = await db.Sales
            .Where(s => s.BusinessId == query.BusinessId
                     && s.BranchId == query.BranchId
                     && ((s.SoldAtUtc >= fromUtc && s.SoldAtUtc < toUtc)
                         || (s.CancelledAtUtc != null && s.CancelledAtUtc >= fromUtc && s.CancelledAtUtc < toUtc)))
            .ToListAsync(ct);

        foreach (var sale in sales)
        {
            if (sale.SoldAtUtc >= fromUtc && sale.SoldAtUtc < toUtc)
            {
                entries.Add(new OperationalAuditEntryDto(
                    sale.SoldAtUtc,
                    sale.UpdatedBy ?? sale.CreatedBy ?? "system",
                    "Ventas",
                    sale.Status == SaleStatus.Cancelled ? "Venta registrada luego anulada" : "Venta registrada",
                    "Sale",
                    sale.Id,
                    sale.ReceiptNumber,
                    sale.Total,
                    $"Estado: {sale.Status}"));
            }

            if (sale.CancelledAtUtc is not null && sale.CancelledAtUtc >= fromUtc && sale.CancelledAtUtc < toUtc)
            {
                entries.Add(new OperationalAuditEntryDto(
                    sale.CancelledAtUtc.Value,
                    sale.CancelledBy ?? sale.UpdatedBy ?? "system",
                    "Ventas",
                    "Venta anulada",
                    "Sale",
                    sale.Id,
                    sale.ReceiptNumber,
                    sale.Total,
                    sale.CancellationReason ?? string.Empty));
            }
        }

        var cashMovements = await db.CashMovements
            .Where(m => m.BusinessId == query.BusinessId
                     && m.BranchId == query.BranchId
                     && m.CreatedAtUtc >= fromUtc
                     && m.CreatedAtUtc < toUtc)
            .ToListAsync(ct);

        entries.AddRange(cashMovements.Select(m => new OperationalAuditEntryDto(
            m.CreatedAtUtc,
            m.CreatedBy ?? "system",
            "Caja",
            GetCashAction(m.MovementType),
            "CashMovement",
            m.Id,
            m.ReferenceType ?? m.MovementType.ToString(),
            m.Amount,
            m.Description ?? string.Empty)));

        var inventoryMovements = await db.InventoryMovements
            .Include(m => m.Product)
            .Where(m => m.BusinessId == query.BusinessId
                     && m.BranchId == query.BranchId
                     && m.CreatedAtUtc >= fromUtc
                     && m.CreatedAtUtc < toUtc)
            .ToListAsync(ct);

        entries.AddRange(inventoryMovements.Select(m => new OperationalAuditEntryDto(
            m.CreatedAtUtc,
            m.CreatedBy ?? "system",
            "Inventario",
            GetInventoryAction(m.MovementType),
            "InventoryMovement",
            m.Id,
            m.Product.Code,
            m.Quantity,
            $"{m.Product.Name}: {m.PreviousQuantity:N2} -> {m.NewQuantity:N2}. {m.Reason}")));

        var expenses = await db.Expenses
            .Where(e => e.BusinessId == query.BusinessId
                     && e.BranchId == query.BranchId
                     && e.CreatedAtUtc >= fromUtc
                     && e.CreatedAtUtc < toUtc)
            .ToListAsync(ct);

        entries.AddRange(expenses.Select(e => new OperationalAuditEntryDto(
            e.CreatedAtUtc,
            e.CreatedBy ?? "system",
            "Gastos",
            "Gasto registrado",
            "Expense",
            e.Id,
            e.Category,
            e.Amount,
            e.Concept)));

        var productions = await db.ProductionEntries
            .Where(p => p.BusinessId == query.BusinessId
                     && p.BranchId == query.BranchId
                     && ((p.ConfirmedAtUtc != null && p.ConfirmedAtUtc >= fromUtc && p.ConfirmedAtUtc < toUtc)
                         || (p.UpdatedAtUtc != null && p.UpdatedAtUtc >= fromUtc && p.UpdatedAtUtc < toUtc)))
            .ToListAsync(ct);

        foreach (var production in productions)
        {
            if (production.ConfirmedAtUtc is not null && production.ConfirmedAtUtc >= fromUtc && production.ConfirmedAtUtc < toUtc)
            {
                entries.Add(new OperationalAuditEntryDto(
                    production.ConfirmedAtUtc.Value,
                    production.ConfirmedBy ?? production.CreatedBy ?? "system",
                    "Produccion",
                    "Produccion confirmada",
                    "ProductionEntry",
                    production.Id,
                    production.Number,
                    null,
                    production.Notes ?? string.Empty));
            }

            if (production.Status == ProductionEntryStatus.Cancelled
                && production.UpdatedAtUtc is not null
                && production.UpdatedAtUtc >= fromUtc
                && production.UpdatedAtUtc < toUtc)
            {
                entries.Add(new OperationalAuditEntryDto(
                    production.UpdatedAtUtc.Value,
                    production.UpdatedBy ?? "system",
                    "Produccion",
                    "Produccion anulada",
                    "ProductionEntry",
                    production.Id,
                    production.Number,
                    null,
                    production.Notes ?? string.Empty));
            }
        }

        var reprints = await db.ReceiptReprintAudits
            .Where(r => r.BusinessId == query.BusinessId
                     && r.BranchId == query.BranchId
                     && r.ReprintedAtUtc >= fromUtc
                     && r.ReprintedAtUtc < toUtc)
            .ToListAsync(ct);

        entries.AddRange(reprints.Select(r => new OperationalAuditEntryDto(
            r.ReprintedAtUtc,
            r.ReprintedBy,
            "Tickets",
            "Ticket reimpreso",
            "ReceiptReprintAudit",
            r.Id,
            r.SaleNumber,
            null,
            r.Source)));

        var filtered = entries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.User))
        {
            var user = query.User.Trim();
            filtered = filtered.Where(e => e.User.Contains(user, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            var area = query.Area.Trim();
            filtered = filtered.Where(e => e.Area.Equals(area, StringComparison.OrdinalIgnoreCase));
        }

        var take = Math.Clamp(query.Take, 1, 1000);
        return Result.Success<IReadOnlyList<OperationalAuditEntryDto>>(
            filtered
                .OrderByDescending(e => e.OccurredAtUtc)
                .ThenBy(e => e.Area)
                .Take(take)
                .ToList());
    }

    private static string GetCashAction(CashMovementType type) => type switch
    {
        CashMovementType.Opening => "Caja abierta",
        CashMovementType.SaleIncome => "Ingreso por venta",
        CashMovementType.OtherIncome => "Otro ingreso",
        CashMovementType.Expense => "Gasto desde caja",
        CashMovementType.Withdrawal => "Retiro de caja",
        CashMovementType.Refund => "Reembolso",
        CashMovementType.ClosingAdjustment => "Ajuste de cierre",
        _ => type.ToString()
    };

    private static string GetInventoryAction(MovementType type) => type switch
    {
        MovementType.Sale => "Salida por venta",
        MovementType.SaleCancellation => "Reverso por anulacion",
        MovementType.AdjustmentIncrease => "Ajuste positivo",
        MovementType.AdjustmentDecrease => "Ajuste negativo",
        MovementType.Waste => "Merma",
        MovementType.ProductionInput => "Insumo consumido",
        MovementType.ProductionOutput => "Produccion agregada",
        MovementType.ProductionCancellation => "Reverso de produccion",
        _ => type.ToString()
    };
}

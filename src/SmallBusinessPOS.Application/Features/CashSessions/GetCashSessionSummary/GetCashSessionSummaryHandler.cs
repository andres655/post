using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionSummary;

public sealed class GetCashSessionSummaryHandler(IAppDbContext db)
{
    public async Task<Result<CashSessionSummaryDto?>> HandleAsync(
        GetCashSessionSummaryQuery query,
        CancellationToken ct = default)
    {
        var session = await db.CashSessions
            .Include(s => s.CashRegister)
            .Where(s => s.CashRegisterId == query.CashRegisterId
                     && s.Status == CashSessionStatus.Open)
            .OrderByDescending(s => s.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (session is null)
            return Result.Success<CashSessionSummaryDto?>(null);

        var movements = await db.CashMovements
            .Where(m => m.CashSessionId == session.Id)
            .ToListAsync(ct);

        var cashSales = movements
            .Where(m => m.MovementType == CashMovementType.SaleIncome)
            .Sum(m => m.Amount);

        var otherIncome = movements
            .Where(m => m.MovementType == CashMovementType.OtherIncome)
            .Sum(m => m.Amount);

        var expenses = movements
            .Where(m => m.MovementType == CashMovementType.Expense)
            .Sum(m => m.Amount);

        var withdrawals = movements
            .Where(m => m.MovementType == CashMovementType.Withdrawal)
            .Sum(m => m.Amount);

        var refunds = movements
            .Where(m => m.MovementType == CashMovementType.Refund)
            .Sum(m => m.Amount);

        var closingAdjustments = movements
            .Where(m => m.MovementType == CashMovementType.ClosingAdjustment)
            .Sum(m => m.Amount);

        return Result.Success<CashSessionSummaryDto?>(new CashSessionSummaryDto(
            session.Id,
            session.CashRegisterId,
            session.CashRegister.Code,
            session.CashRegister.Name,
            session.OpenedAtUtc,
            session.ClosedAtUtc,
            session.Status.ToString(),
            session.OpeningBalance,
            cashSales,
            otherIncome,
            expenses,
            withdrawals,
            refunds,
            closingAdjustments,
            session.ClosingBalance,
            session.Status == CashSessionStatus.Closed ? session.DeclaredClosingBalance : null,
            session.Status == CashSessionStatus.Closed ? session.Difference : null));
    }
}

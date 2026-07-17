using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionSummary;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionHistory;

public sealed class GetCashSessionHistoryHandler(IAppDbContext db)
{
    public async Task<Result<IReadOnlyList<CashSessionSummaryDto>>> HandleAsync(
        GetCashSessionHistoryQuery query,
        CancellationToken ct = default)
    {
        var sessionsQuery = db.CashSessions
            .Include(s => s.CashRegister)
            .Where(s => s.CashRegisterId == query.CashRegisterId
                     && s.Status == CashSessionStatus.Closed);

        if (query.From is not null)
        {
            var from = query.From.Value.ToDateTime(TimeOnly.MinValue);
            sessionsQuery = sessionsQuery.Where(s => s.ClosedAtUtc >= from);
        }

        if (query.To is not null)
        {
            var to = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            sessionsQuery = sessionsQuery.Where(s => s.ClosedAtUtc < to);
        }

        var sessions = await sessionsQuery
            .OrderByDescending(s => s.ClosedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        if (sessions.Count == 0)
            return Result.Success<IReadOnlyList<CashSessionSummaryDto>>([]);

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var movements = await db.CashMovements
            .Where(m => sessionIds.Contains(m.CashSessionId))
            .ToListAsync(ct);

        var summaries = sessions
            .Select(session => BuildSummary(session, movements.Where(m => m.CashSessionId == session.Id)))
            .ToList();

        return Result.Success<IReadOnlyList<CashSessionSummaryDto>>(summaries);
    }

    private static CashSessionSummaryDto BuildSummary(CashSession session, IEnumerable<CashMovement> movements)
    {
        var movementList = movements.ToList();
        var cashSales = movementList.Where(m => m.MovementType == CashMovementType.SaleIncome).Sum(m => m.Amount);
        var otherIncome = movementList.Where(m => m.MovementType == CashMovementType.OtherIncome).Sum(m => m.Amount);
        var expenses = movementList.Where(m => m.MovementType == CashMovementType.Expense).Sum(m => m.Amount);
        var withdrawals = movementList.Where(m => m.MovementType == CashMovementType.Withdrawal).Sum(m => m.Amount);
        var refunds = movementList.Where(m => m.MovementType == CashMovementType.Refund).Sum(m => m.Amount);
        var closingAdjustments = movementList.Where(m => m.MovementType == CashMovementType.ClosingAdjustment).Sum(m => m.Amount);

        return new CashSessionSummaryDto(
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
            session.DeclaredClosingBalance,
            session.Difference);
    }
}

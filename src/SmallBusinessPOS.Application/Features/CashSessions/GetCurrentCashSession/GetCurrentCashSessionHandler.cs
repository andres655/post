using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.CashSessions.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.CashSessions.GetCurrentCashSession;

public sealed class GetCurrentCashSessionHandler(IAppDbContext db)
{
    public async Task<Result<CashSessionDto?>> HandleAsync(
        GetCurrentCashSessionQuery query,
        CancellationToken ct = default)
    {
        var session = await db.CashSessions
            .Include(s => s.CashRegister)
            .Where(s => s.CashRegisterId == query.CashRegisterId && s.Status == CashSessionStatus.Open)
            .OrderByDescending(s => s.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (session is null)
            return Result.Success<CashSessionDto?>(null);

        return Result.Success<CashSessionDto?>(new CashSessionDto(
            session.Id,
            session.CashRegisterId,
            session.CashRegister.Code,
            session.CashRegister.Name,
            session.OpenedAtUtc,
            session.OpeningBalance,
            session.TotalIncome,
            session.TotalExpenses,
            session.ClosingBalance,
            null,
            null,
            session.Status.ToString()));
    }
}

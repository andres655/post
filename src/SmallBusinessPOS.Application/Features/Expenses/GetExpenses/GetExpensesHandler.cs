using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Expenses.GetExpenses;

public sealed class GetExpensesHandler(IAppDbContext db)
{
    public async Task<Result<List<ExpenseDto>>> HandleAsync(
        GetExpensesQuery query,
        CancellationToken ct = default)
    {
        if (query.ToDate < query.FromDate)
            return Result.Failure<List<ExpenseDto>>(
                Error.Validation("ToDate", "La fecha final debe ser mayor o igual que la fecha inicial."));

        var fromUtc = query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = query.ToDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var expenses = await db.Expenses
            .Where(e => e.BusinessId == query.BusinessId
                     && e.BranchId == query.BranchId
                     && e.CreatedAtUtc >= fromUtc
                     && e.CreatedAtUtc < toUtc)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(query.MaxRows)
            .Select(e => new ExpenseDto(
                e.Id,
                e.CreatedAtUtc,
                e.Category,
                e.Concept,
                e.Amount,
                e.CashSessionId != null,
                e.Notes,
                e.CreatedBy))
            .ToListAsync(ct);

        return Result.Success(expenses);
    }
}

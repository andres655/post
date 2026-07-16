namespace SmallBusinessPOS.Application.Features.Expenses.GetExpenses;

public sealed record GetExpensesQuery(
    Guid BusinessId,
    Guid BranchId,
    DateOnly FromDate,
    DateOnly ToDate,
    int MaxRows = 100);

public sealed record ExpenseDto(
    Guid ExpenseId,
    DateTime CreatedAtUtc,
    string Category,
    string Concept,
    decimal Amount,
    bool PaidFromCash,
    string? Notes,
    string? CreatedBy);

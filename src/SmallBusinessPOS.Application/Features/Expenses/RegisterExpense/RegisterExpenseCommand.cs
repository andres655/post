namespace SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;

public sealed record RegisterExpenseCommand(
    Guid BusinessId,
    Guid BranchId,
    string Category,
    string Concept,
    decimal Amount,
    bool PaidFromCash,
    string? Notes = null);

public sealed record RegisterExpenseResultDto(
    Guid ExpenseId,
    Guid? CashSessionId,
    decimal Amount,
    decimal? ExpectedCash);

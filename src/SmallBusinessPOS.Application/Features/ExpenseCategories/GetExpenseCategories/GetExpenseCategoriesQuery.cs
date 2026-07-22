namespace SmallBusinessPOS.Application.Features.ExpenseCategories.GetExpenseCategories;

public sealed record GetExpenseCategoriesQuery(
    Guid BusinessId,
    bool OnlyActive = true,
    int MaxRows = 200);

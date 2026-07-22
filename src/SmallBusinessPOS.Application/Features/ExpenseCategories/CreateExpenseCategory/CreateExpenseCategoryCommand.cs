namespace SmallBusinessPOS.Application.Features.ExpenseCategories.CreateExpenseCategory;

public sealed record CreateExpenseCategoryCommand(
    Guid BusinessId,
    string Name,
    string? Description = null,
    int SortOrder = 0);

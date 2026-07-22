namespace SmallBusinessPOS.Application.Features.ExpenseCategories.DTOs;

public sealed record ExpenseCategoryDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);

using FluentValidation;

namespace SmallBusinessPOS.Application.Features.ExpenseCategories.CreateExpenseCategory;

public class CreateExpenseCategoryValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

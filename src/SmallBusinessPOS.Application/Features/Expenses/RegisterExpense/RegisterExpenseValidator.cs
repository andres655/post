using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;

public class RegisterExpenseValidator : AbstractValidator<RegisterExpenseCommand>
{
    public RegisterExpenseValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Concept).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

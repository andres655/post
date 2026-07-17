using FluentValidation;

namespace SmallBusinessPOS.Application.Features.CashSessions.RegisterCashWithdrawal;

public sealed class RegisterCashWithdrawalValidator : AbstractValidator<RegisterCashWithdrawalCommand>
{
    public RegisterCashWithdrawalValidator()
    {
        RuleFor(x => x.CashRegisterId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

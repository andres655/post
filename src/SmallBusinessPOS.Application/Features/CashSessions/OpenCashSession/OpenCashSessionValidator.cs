using FluentValidation;

namespace SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;

public sealed class OpenCashSessionValidator : AbstractValidator<OpenCashSessionCommand>
{
    public OpenCashSessionValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CashRegisterId).NotEmpty();
        RuleFor(x => x.OpeningAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El monto inicial no puede ser negativo.");
    }
}

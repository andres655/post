using FluentValidation;

namespace SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;

public sealed class CloseCashSessionValidator : AbstractValidator<CloseCashSessionCommand>
{
    public CloseCashSessionValidator()
    {
        RuleFor(x => x.CashSessionId).NotEmpty();
        RuleFor(x => x.CountedAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El efectivo contado no puede ser negativo.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

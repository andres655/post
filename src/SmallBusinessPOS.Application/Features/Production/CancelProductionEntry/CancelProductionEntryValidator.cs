using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Production.CancelProductionEntry;

public sealed class CancelProductionEntryValidator : AbstractValidator<CancelProductionEntryCommand>
{
    public CancelProductionEntryValidator()
    {
        RuleFor(x => x.ProductionEntryId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;

public class ConfirmProductionEntryValidator : AbstractValidator<ConfirmProductionEntryCommand>
{
    public ConfirmProductionEntryValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ProductionDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines)
            .NotEmpty()
            .When(x => x.ProductionEntryId is null)
            .WithMessage("Debe indicar al menos un producto producido.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.ProductId).NotEmpty();
            line.RuleFor(x => x.QuantityProduced).GreaterThan(0);
            line.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.QuantityWasted).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.QuantityWasted)
                .LessThanOrEqualTo(x => x.QuantityProduced)
                .WithMessage("La merma no puede ser mayor que la cantidad producida.");
        });

        RuleForEach(x => x.Inputs).ChildRules(input =>
        {
            input.RuleFor(x => x.ProductId).NotEmpty();
            input.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

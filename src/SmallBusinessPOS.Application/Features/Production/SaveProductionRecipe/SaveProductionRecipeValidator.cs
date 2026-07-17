using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Production.SaveProductionRecipe;

public sealed class SaveProductionRecipeValidator : AbstractValidator<SaveProductionRecipeCommand>
{
    public SaveProductionRecipeValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.ParentProductId).NotEmpty();

        RuleForEach(x => x.Components).ChildRules(component =>
        {
            component.RuleFor(x => x.ProductId).NotEmpty();
            component.RuleFor(x => x.Quantity).GreaterThan(0m);
        });
    }
}

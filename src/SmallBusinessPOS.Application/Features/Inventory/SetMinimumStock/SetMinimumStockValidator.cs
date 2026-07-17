using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Inventory.SetMinimumStock;

public sealed class SetMinimumStockValidator : AbstractValidator<SetMinimumStockCommand>
{
    public SetMinimumStockValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.MinimumQuantity).GreaterThanOrEqualTo(0m);
    }
}

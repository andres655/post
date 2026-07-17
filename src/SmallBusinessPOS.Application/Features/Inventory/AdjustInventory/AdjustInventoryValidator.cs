using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Inventory.AdjustInventory;

public sealed class AdjustInventoryValidator : AbstractValidator<AdjustInventoryCommand>
{
    public AdjustInventoryValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).NotEqual(0m);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

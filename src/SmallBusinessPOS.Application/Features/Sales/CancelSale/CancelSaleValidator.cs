using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Sales.CancelSale;

public sealed class CancelSaleValidator : AbstractValidator<CancelSaleCommand>
{
    public CancelSaleValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo de cancelación es obligatorio.")
            .MaximumLength(500);
    }
}

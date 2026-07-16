using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Sales.CreateSale;

public sealed class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CashRegisterId).NotEmpty();

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El descuento no puede ser negativo.");

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El impuesto no puede ser negativo.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("La venta debe tener al menos una línea.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.ProductId).NotEmpty();
            line.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("La cantidad debe ser mayor a cero.");
            line.RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("El precio unitario no puede ser negativo.");
        });

        RuleFor(x => x.Payments)
            .NotEmpty().WithMessage("La venta debe tener al menos un pago.");

        RuleForEach(x => x.Payments).ChildRules(payment =>
        {
            payment.RuleFor(x => x.PaymentMethodId).NotEmpty();
            payment.RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("El monto del pago debe ser mayor a cero.");
        });
    }
}

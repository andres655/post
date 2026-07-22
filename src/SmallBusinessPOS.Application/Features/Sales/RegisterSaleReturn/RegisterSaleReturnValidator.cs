using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Sales.RegisterSaleReturn;

public sealed class RegisterSaleReturnValidator : AbstractValidator<RegisterSaleReturnCommand>
{
    public RegisterSaleReturnValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.RefundPaymentMethodId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Debe indicar el motivo de la devolucion.").MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Debe indicar al menos una linea a devolver.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.SaleDetailId).NotEmpty();
            line.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad devuelta debe ser mayor a cero.");
        });
    }
}

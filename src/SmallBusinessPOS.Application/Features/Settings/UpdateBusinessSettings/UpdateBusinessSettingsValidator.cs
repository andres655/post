using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Settings.UpdateBusinessSettings;

public sealed class UpdateBusinessSettingsValidator : AbstractValidator<UpdateBusinessSettingsCommand>
{
    public UpdateBusinessSettingsValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Debe indicar el nombre del negocio.")
            .MaximumLength(200);
        RuleFor(x => x.BranchName)
            .NotEmpty().WithMessage("Debe indicar el nombre de la sucursal.")
            .MaximumLength(120);
        RuleFor(x => x.CurrencySymbol)
            .NotEmpty().WithMessage("Debe indicar el simbolo de moneda.")
            .MaximumLength(10);
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Debe indicar el codigo de moneda.")
            .MaximumLength(10);
        RuleFor(x => x.DefaultTaxRate)
            .InclusiveBetween(0, 100)
            .WithMessage("El impuesto debe estar entre 0 y 100.");
    }
}

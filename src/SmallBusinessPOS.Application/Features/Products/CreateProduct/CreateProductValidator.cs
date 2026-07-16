using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Products.CreateProduct;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("El negocio es obligatorio.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código del producto es obligatorio.")
            .MaximumLength(50).WithMessage("El código no puede superar 50 caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de venta no puede ser negativo.");

        RuleFor(x => x.EstimatedCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo estimado no puede ser negativo.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("La descripción no puede superar 1000 caracteres.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("El código de barras no puede superar 100 caracteres.")
            .When(x => x.Barcode is not null);
    }
}

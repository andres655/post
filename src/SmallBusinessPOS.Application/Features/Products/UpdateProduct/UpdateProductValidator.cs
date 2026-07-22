using FluentValidation;

namespace SmallBusinessPOS.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El Id del producto es obligatorio.");

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
        RuleFor(x => x.InventorySourceQuantity)
            .GreaterThan(0m).WithMessage("La cantidad descontada del producto base debe ser mayor que cero.")
            .When(x => x.InventorySourceProductId.HasValue);

        RuleForEach(x => x.InventoryComponents)
            .ChildRules(component =>
            {
                component.RuleFor(x => x.ProductId).NotEmpty();
                component.RuleFor(x => x.Quantity)
                    .GreaterThan(0m).WithMessage("La cantidad del componente debe ser mayor que cero.");
            })
            .When(x => x.InventoryComponents is not null);
    }
}

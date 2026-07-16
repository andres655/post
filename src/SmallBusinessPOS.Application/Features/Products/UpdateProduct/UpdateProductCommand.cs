using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.UpdateProduct;

/// <summary>Comando para actualizar un producto existente.</summary>
public sealed record UpdateProductCommand(
    Guid Id,
    string Code,
    string Name,
    ProductType ProductType,
    UnitOfMeasure UnitOfMeasure,
    decimal SalePrice,
    decimal EstimatedCost,
    Guid? CategoryId,
    bool TracksInventory,
    bool AllowsFractionalQuantity,
    string? Description = null,
    string? Barcode = null);

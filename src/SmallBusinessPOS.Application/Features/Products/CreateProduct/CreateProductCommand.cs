using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.CreateProduct;

/// <summary>Comando para crear un nuevo producto.</summary>
public sealed record CreateProductCommand(
    Guid BusinessId,
    string Code,
    string Name,
    ProductType ProductType,
    UnitOfMeasure UnitOfMeasure,
    decimal SalePrice,
    decimal EstimatedCost = 0m,
    Guid? CategoryId = null,
    bool TracksInventory = true,
    bool AllowsFractionalQuantity = false,
    string? Description = null,
    string? Barcode = null,
    Guid? InventorySourceProductId = null,
    decimal? InventorySourceQuantity = null,
    IReadOnlyList<ProductInventoryComponentInput>? InventoryComponents = null);

public sealed record ProductInventoryComponentInput(
    Guid ProductId,
    decimal Quantity);

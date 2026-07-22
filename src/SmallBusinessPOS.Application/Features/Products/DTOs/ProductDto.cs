using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.DTOs;

/// <summary>DTO completo de producto para detalle y edición.</summary>
public sealed record ProductDto(
    Guid Id,
    Guid BusinessId,
    Guid? CategoryId,
    string? CategoryName,
    string Code,
    string? Barcode,
    string Name,
    string? Description,
    ProductType ProductType,
    string ProductTypeName,
    UnitOfMeasure UnitOfMeasure,
    string UnitOfMeasureName,
    decimal SalePrice,
    decimal EstimatedCost,
    bool TracksInventory,
    bool AllowsFractionalQuantity,
    Guid? InventorySourceProductId,
    string? InventorySourceProductName,
    decimal? InventorySourceQuantity,
    IReadOnlyList<ProductInventoryComponentDto> InventoryComponents,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record ProductInventoryComponentDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity);

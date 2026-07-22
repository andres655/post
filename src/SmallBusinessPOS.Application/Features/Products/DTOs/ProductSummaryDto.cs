using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.DTOs;

/// <summary>DTO reducido de producto para listas y búsquedas.</summary>
public sealed record ProductSummaryDto(
    Guid Id,
    string Code,
    string? Barcode,
    string Name,
    string? CategoryName,
    ProductType ProductType,
    decimal SalePrice,
    bool IsActive,
    bool TracksInventory = true,
    bool AllowsFractionalQuantity = false);

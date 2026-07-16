using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.DTOs;

/// <summary>DTO reducido de producto para listas y búsquedas.</summary>
public sealed record ProductSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? CategoryName,
    ProductType ProductType,
    decimal SalePrice,
    bool IsActive);

using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.ProductTypes.DTOs;

public sealed record ProductTypeOptionDto(
    Guid Id,
    Guid BusinessId,
    ProductType Value,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);

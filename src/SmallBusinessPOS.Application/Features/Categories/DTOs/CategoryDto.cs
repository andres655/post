namespace SmallBusinessPOS.Application.Features.Categories.DTOs;

/// <summary>DTO de categoría para transferencia entre capas.</summary>
public sealed record CategoryDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

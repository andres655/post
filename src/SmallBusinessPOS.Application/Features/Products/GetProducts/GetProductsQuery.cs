using SmallBusinessPOS.Application.Features.Products.DTOs;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.GetProducts;

/// <summary>Consulta para obtener productos de un negocio con filtros opcionales.</summary>
public sealed record GetProductsQuery(
    Guid BusinessId,
    bool OnlyActive = true,
    Guid? CategoryId = null,
    ProductType? ProductType = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 5);

public sealed record ProductsPageDto(
    IReadOnlyList<ProductSummaryDto> Items,
    int TotalCount);

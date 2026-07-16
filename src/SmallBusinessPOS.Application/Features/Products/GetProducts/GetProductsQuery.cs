using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Products.GetProducts;

/// <summary>Consulta para obtener productos de un negocio con filtros opcionales.</summary>
public sealed record GetProductsQuery(
    Guid BusinessId,
    bool OnlyActive = true,
    Guid? CategoryId = null,
    ProductType? ProductType = null,
    string? SearchTerm = null);

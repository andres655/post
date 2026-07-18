namespace SmallBusinessPOS.Application.Features.Categories.GetCategories;

/// <summary>Consulta para obtener todas las categorías de un negocio.</summary>
public sealed record GetCategoriesQuery(Guid BusinessId, bool OnlyActive = true, int MaxRows = 200);

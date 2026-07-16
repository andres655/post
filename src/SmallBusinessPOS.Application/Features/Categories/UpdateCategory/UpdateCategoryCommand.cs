namespace SmallBusinessPOS.Application.Features.Categories.UpdateCategory;

/// <summary>Comando para actualizar una categoría existente.</summary>
public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder);

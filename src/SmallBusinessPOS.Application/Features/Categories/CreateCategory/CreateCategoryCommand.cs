namespace SmallBusinessPOS.Application.Features.Categories.CreateCategory;

/// <summary>Comando para crear una nueva categoría.</summary>
public sealed record CreateCategoryCommand(
    Guid BusinessId,
    string Name,
    string? Description,
    int SortOrder = 0);

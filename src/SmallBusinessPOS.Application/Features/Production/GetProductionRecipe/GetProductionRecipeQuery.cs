namespace SmallBusinessPOS.Application.Features.Production.GetProductionRecipe;

public sealed record GetProductionRecipeQuery(
    Guid BusinessId,
    Guid ParentProductId);

public sealed record ProductionRecipeDto(
    Guid ParentProductId,
    IReadOnlyList<ProductionRecipeComponentDto> Components);

public sealed record ProductionRecipeComponentDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity);

namespace SmallBusinessPOS.Application.Features.Production.SaveProductionRecipe;

public sealed record SaveProductionRecipeCommand(
    Guid BusinessId,
    Guid ParentProductId,
    IReadOnlyList<SaveProductionRecipeComponent> Components);

public sealed record SaveProductionRecipeComponent(
    Guid ProductId,
    decimal Quantity);

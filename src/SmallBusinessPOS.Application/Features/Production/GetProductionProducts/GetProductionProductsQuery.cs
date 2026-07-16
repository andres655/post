namespace SmallBusinessPOS.Application.Features.Production.GetProductionProducts;

public sealed record GetProductionProductsQuery(Guid BusinessId);

public sealed record ProductionProductDto(
    Guid ProductId,
    string Code,
    string Name,
    decimal EstimatedCost);

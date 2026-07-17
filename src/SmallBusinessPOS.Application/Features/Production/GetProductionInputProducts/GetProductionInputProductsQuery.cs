namespace SmallBusinessPOS.Application.Features.Production.GetProductionInputProducts;

public sealed record GetProductionInputProductsQuery(Guid BusinessId);

public sealed record ProductionInputProductDto(
    Guid ProductId,
    string Code,
    string Name);

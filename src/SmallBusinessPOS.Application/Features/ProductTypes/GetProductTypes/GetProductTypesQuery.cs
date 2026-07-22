namespace SmallBusinessPOS.Application.Features.ProductTypes.GetProductTypes;

public sealed record GetProductTypesQuery(
    Guid BusinessId,
    bool OnlyActive = true,
    int MaxRows = 100);

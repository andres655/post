using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.ProductTypes.DTOs;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.ProductTypes.GetProductTypes;

public sealed class GetProductTypesHandler(IAppDbContext db)
{
    public async Task<Result<List<ProductTypeOptionDto>>> HandleAsync(
        GetProductTypesQuery query,
        CancellationToken ct = default)
    {
        var take = Math.Clamp(query.MaxRows, 1, 100);

        var optionsQuery = db.ProductTypeOptions
            .Where(t => t.BusinessId == query.BusinessId);

        if (query.OnlyActive)
            optionsQuery = optionsQuery.Where(t => t.IsActive);

        var options = await optionsQuery
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .Take(take)
            .Select(t => new ProductTypeOptionDto(
                t.Id,
                t.BusinessId,
                t.Value,
                t.Name,
                t.Description,
                t.SortOrder,
                t.IsActive))
            .ToListAsync(ct);

        return Result.Success(options);
    }
}

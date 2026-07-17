using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Production.GetProductionHistory;

public sealed class GetProductionHistoryHandler(IAppDbContext db)
{
    public async Task<Result<IReadOnlyList<ProductionHistoryDto>>> HandleAsync(
        GetProductionHistoryQuery query,
        CancellationToken ct = default)
    {
        var entriesQuery = db.ProductionEntries
            .Include(e => e.Details)
                .ThenInclude(d => d.Product)
            .Where(e => e.BusinessId == query.BusinessId
                     && e.BranchId == query.BranchId);

        if (query.From is not null)
            entriesQuery = entriesQuery.Where(e => e.ProductionDate >= query.From.Value);

        if (query.To is not null)
            entriesQuery = entriesQuery.Where(e => e.ProductionDate <= query.To.Value);

        var entries = await entriesQuery
            .OrderByDescending(e => e.ProductionDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        var entryIds = entries.Select(e => e.Id).ToList();
        var inputMovements = await db.InventoryMovements
            .Include(m => m.Product)
            .Where(m => m.ReferenceType == "ProductionEntry"
                     && m.ReferenceId != null
                     && entryIds.Contains(m.ReferenceId.Value)
                     && m.MovementType == MovementType.ProductionInput)
            .ToListAsync(ct);

        var result = entries.Select(e =>
        {
            var details = e.Details
                .OrderBy(d => d.Product.Code)
                .Select(d => new ProductionHistoryDetailDto(
                    d.ProductId,
                    d.Product.Code,
                    d.Product.Name,
                    d.QuantityProduced,
                    d.QuantityWasted,
                    d.QuantityProduced - d.QuantityWasted,
                    d.UnitCost,
                    d.QuantityProduced * d.UnitCost))
                .ToList();

            var inputs = inputMovements
                .Where(m => m.ReferenceId == e.Id)
                .GroupBy(m => new { m.ProductId, m.Product.Code, m.Product.Name, m.Product.EstimatedCost })
                .Select(g => new ProductionHistoryInputDto(
                    g.Key.ProductId,
                    g.Key.Code,
                    g.Key.Name,
                    g.Sum(m => m.Quantity),
                    g.Key.EstimatedCost,
                    g.Sum(m => m.Quantity) * g.Key.EstimatedCost))
                .OrderBy(i => i.ProductCode)
                .ToList();

            var directCost = details.Sum(d => d.TotalCost);
            var inputCost = inputs.Sum(i => i.TotalCost);
            var totalCost = directCost + inputCost;
            var netAdded = details.Sum(d => d.NetQuantity);

            return new ProductionHistoryDto(
                e.Id,
                e.Number,
                e.ProductionDate,
                e.Status.ToString(),
                details.Sum(d => d.QuantityProduced),
                details.Sum(d => d.QuantityWasted),
                netAdded,
                directCost,
                inputCost,
                totalCost,
                netAdded == 0m ? 0m : totalCost / netAdded,
                e.Notes,
                e.ConfirmedAtUtc,
                details,
                inputs);
        }).ToList();

        return Result.Success<IReadOnlyList<ProductionHistoryDto>>(result);
    }
}

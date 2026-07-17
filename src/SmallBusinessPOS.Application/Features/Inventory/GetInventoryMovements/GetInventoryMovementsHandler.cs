using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.Inventory.GetInventoryMovements;

public sealed class GetInventoryMovementsHandler(IAppDbContext db)
{
    public async Task<Result<IReadOnlyList<InventoryMovementDto>>> HandleAsync(
        GetInventoryMovementsQuery query,
        CancellationToken ct = default)
    {
        var take = Math.Clamp(query.Take, 1, 200);

        var movements = await db.InventoryMovements
            .Where(m => m.BusinessId == query.BusinessId
                     && m.BranchId == query.BranchId
                     && m.ProductId == query.ProductId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(take)
            .Select(m => new InventoryMovementDto(
                m.Id,
                m.CreatedAtUtc,
                m.MovementType,
                m.Quantity,
                m.PreviousQuantity,
                m.NewQuantity,
                m.Reason,
                m.ReferenceType))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<InventoryMovementDto>>(movements);
    }
}

using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;

namespace SmallBusinessPOS.Application.Features.POS.GetPosOptions;

public sealed class GetPosOptionsHandler(IAppDbContext db)
{
    public async Task<Result<PosOptionsDto>> HandleAsync(GetPosOptionsQuery query, CancellationToken ct = default)
    {
        var branches = await db.Branches
            .Where(branch => branch.BusinessId == query.BusinessId && branch.IsActive)
            .OrderByDescending(branch => branch.IsMain)
            .ThenBy(branch => branch.Name)
            .Select(branch => new PosBranchOptionDto(branch.Id, branch.Name, branch.IsMain))
            .ToListAsync(ct);

        var cashRegisters = await db.CashRegisters
            .Where(register => register.BusinessId == query.BusinessId && register.IsActive)
            .OrderBy(register => register.BranchId)
            .ThenBy(register => register.Code)
            .Select(register => new PosCashRegisterOptionDto(register.Id, register.BranchId, register.Code, register.Name))
            .ToListAsync(ct);

        return Result.Success(new PosOptionsDto(branches, cashRegisters));
    }
}

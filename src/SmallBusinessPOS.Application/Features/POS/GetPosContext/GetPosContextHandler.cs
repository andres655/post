using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.POS.GetPosContext;

public sealed class GetPosContextHandler(IAppDbContext db)
{
    public async Task<Result<PosContextDto>> HandleAsync(
        GetPosContextQuery query,
        CancellationToken ct = default)
    {
        var business = await db.Businesses
            .Where(b => b.IsActive)
            .OrderBy(b => b.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (business is null)
            return Result.Failure<PosContextDto>(Error.NotFound("Business", "No hay negocio configurado."));

        var branch = await db.Branches
            .Where(b => b.BusinessId == business.Id && b.IsActive)
            .OrderByDescending(b => b.IsMain)
            .ThenBy(b => b.Name)
            .FirstOrDefaultAsync(ct);

        if (branch is null)
            return Result.Failure<PosContextDto>(Error.NotFound("Branch", "No hay sucursal activa."));

        var register = await db.CashRegisters
            .Where(r => r.BusinessId == business.Id && r.BranchId == branch.Id && r.IsActive)
            .OrderBy(r => r.Code)
            .FirstOrDefaultAsync(ct);

        if (register is null)
            return Result.Failure<PosContextDto>(Error.NotFound("CashRegister", "No hay caja activa."));

        var openSession = await db.CashSessions
            .Where(s => s.CashRegisterId == register.Id && s.Status == CashSessionStatus.Open)
            .OrderByDescending(s => s.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        return Result.Success(new PosContextDto(
            business.Id,
            business.Name,
            branch.Id,
            branch.Name,
            register.Id,
            register.Code,
            openSession is not null,
            openSession?.Id));
    }
}

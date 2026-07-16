using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.CashSessions.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;

public sealed class CloseCashSessionHandler(
    IAppDbContext db,
    CloseCashSessionValidator validator)
{
    public async Task<Result<CashSessionDto>> HandleAsync(
        CloseCashSessionCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<CashSessionDto>(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var session = await db.CashSessions
            .Include(s => s.CashRegister)
            .FirstOrDefaultAsync(s => s.Id == command.CashSessionId, ct);

        if (session is null)
            return Result.Failure<CashSessionDto>(Error.NotFound("CashSession", command.CashSessionId));

        if (session.Status != CashSessionStatus.Open)
            return Result.Failure<CashSessionDto>(
                Error.BusinessRule("CashSession.NotOpen", "La sesión de caja no está abierta."));

        session.Close(command.CountedAmount, command.Notes, currentUser);

        var adjustment = session.Difference;
        if (adjustment != 0m)
        {
            db.CashMovements.Add(CashMovement.Create(
                session.BusinessId,
                session.BranchId,
                session.Id,
                CashMovementType.ClosingAdjustment,
                Math.Abs(adjustment),
                adjustment >= 0 ? "Ajuste de cierre (sobrante)" : "Ajuste de cierre (faltante)",
                referenceType: "CashSession",
                referenceId: session.Id,
                createdBy: currentUser));
        }

        await db.SaveChangesAsync(ct);

        return Result.Success(new CashSessionDto(
            session.Id,
            session.CashRegisterId,
            session.CashRegister.Code,
            session.CashRegister.Name,
            session.OpenedAtUtc,
            session.OpeningBalance,
            session.TotalIncome,
            session.TotalExpenses,
            session.ClosingBalance,
            session.DeclaredClosingBalance,
            session.Difference,
            session.Status.ToString()));
    }
}

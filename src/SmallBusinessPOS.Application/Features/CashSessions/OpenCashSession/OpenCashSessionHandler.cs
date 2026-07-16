using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Features.CashSessions.DTOs;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;

public sealed class OpenCashSessionHandler(
    IAppDbContext db,
    OpenCashSessionValidator validator)
{
    public async Task<Result<CashSessionDto>> HandleAsync(
        OpenCashSessionCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<CashSessionDto>(Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var register = await db.CashRegisters
            .FirstOrDefaultAsync(r => r.Id == command.CashRegisterId
                                   && r.BusinessId == command.BusinessId
                                   && r.BranchId == command.BranchId
                                   && r.IsActive, ct);

        if (register is null)
            return Result.Failure<CashSessionDto>(Error.NotFound("CashRegister", command.CashRegisterId));

        var alreadyOpen = await db.CashSessions
            .AnyAsync(s => s.CashRegisterId == command.CashRegisterId && s.Status == CashSessionStatus.Open, ct);

        if (alreadyOpen)
            return Result.Failure<CashSessionDto>(
                Error.BusinessRule("CashSession.AlreadyOpen", "La caja ya tiene una sesión abierta."));

        var session = CashSession.Create(
            command.BusinessId,
            command.BranchId,
            command.CashRegisterId,
            command.OpeningAmount,
            currentUser);

        db.CashSessions.Add(session);

        if (command.OpeningAmount > 0)
        {
            var openingMovement = CashMovement.Create(
                command.BusinessId,
                command.BranchId,
                session.Id,
                CashMovementType.Opening,
                command.OpeningAmount,
                "Apertura de caja",
                referenceType: "CashSession",
                referenceId: session.Id,
                createdBy: currentUser);

            db.CashMovements.Add(openingMovement);
        }

        await db.SaveChangesAsync(ct);

        return Result.Success(new CashSessionDto(
            session.Id,
            register.Id,
            register.Code,
            register.Name,
            session.OpenedAtUtc,
            session.OpeningBalance,
            session.TotalIncome,
            session.TotalExpenses,
            session.ClosingBalance,
            null,
            null,
            session.Status.ToString()));
    }
}

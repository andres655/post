using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.CashSessions.RegisterCashWithdrawal;

public sealed class RegisterCashWithdrawalHandler(
    IAppDbContext db,
    RegisterCashWithdrawalValidator validator)
{
    public async Task<Result<RegisterCashWithdrawalResultDto>> HandleAsync(
        RegisterCashWithdrawalCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<RegisterCashWithdrawalResultDto>(
                Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        var session = await db.CashSessions
            .Where(s => s.CashRegisterId == command.CashRegisterId
                     && s.Status == CashSessionStatus.Open)
            .OrderByDescending(s => s.OpenedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (session is null)
            return Result.Failure<RegisterCashWithdrawalResultDto>(
                Error.BusinessRule("CashWithdrawal.CashSessionRequired", "Debe haber una caja abierta para registrar un retiro."));

        if (command.Amount > session.ClosingBalance)
            return Result.Failure<RegisterCashWithdrawalResultDto>(
                Error.BusinessRule("CashWithdrawal.InsufficientCash", "El retiro no puede exceder el efectivo esperado en caja."));

        session.AddExpense(command.Amount);

        var description = string.IsNullOrWhiteSpace(command.Notes)
            ? command.Reason
            : $"{command.Reason}: {command.Notes}";

        var movement = CashMovement.Create(
            session.BusinessId,
            session.BranchId,
            session.Id,
            CashMovementType.Withdrawal,
            command.Amount,
            description,
            referenceType: "CashWithdrawal",
            referenceId: session.Id,
            createdBy: currentUser);

        db.CashMovements.Add(movement);
        await db.SaveChangesAsync(ct);

        return Result.Success(new RegisterCashWithdrawalResultDto(
            movement.Id,
            session.Id,
            movement.Amount,
            session.ClosingBalance));
    }
}

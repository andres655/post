using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Common;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;

public sealed class RegisterExpenseHandler(
    IAppDbContext db,
    RegisterExpenseValidator validator)
{
    public async Task<Result<RegisterExpenseResultDto>> HandleAsync(
        RegisterExpenseCommand command,
        string? currentUser = null,
        CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var error = validation.Errors[0];
            return Result.Failure<RegisterExpenseResultDto>(
                Error.Validation(error.PropertyName, error.ErrorMessage));
        }

        CashSession? cashSession = null;
        if (command.PaidFromCash)
        {
            cashSession = await db.CashSessions
                .Where(s => s.BusinessId == command.BusinessId
                         && s.BranchId == command.BranchId
                         && s.Status == CashSessionStatus.Open)
                .OrderByDescending(s => s.OpenedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (cashSession is null)
                return Result.Failure<RegisterExpenseResultDto>(
                    Error.BusinessRule("Expense.CashSessionRequired", "Debe haber una caja abierta para registrar un gasto pagado desde caja."));
        }

        var categoryName = command.Category.Trim();
        if (command.ExpenseCategoryId.HasValue)
        {
            var category = await db.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Id == command.ExpenseCategoryId.Value
                                       && c.BusinessId == command.BusinessId
                                       && c.IsActive, ct);

            if (category is null)
                return Result.Failure<RegisterExpenseResultDto>(
                    Error.NotFound("ExpenseCategory", command.ExpenseCategoryId.Value));

            categoryName = category.Name;
        }

        var expense = Expense.Create(
            command.BusinessId,
            command.BranchId,
            categoryName,
            command.Concept,
            command.Amount,
            cashSession?.Id,
            command.ExpenseCategoryId,
            command.Notes,
            currentUser);

        db.Expenses.Add(expense);

        if (cashSession is not null)
        {
            cashSession.AddExpense(command.Amount);

            db.CashMovements.Add(CashMovement.Create(
                command.BusinessId,
                command.BranchId,
                cashSession.Id,
                CashMovementType.Expense,
                command.Amount,
                command.Concept,
                referenceType: "Expense",
                referenceId: expense.Id,
                createdBy: currentUser));
        }

        await db.SaveChangesAsync(ct);

        return Result.Success(new RegisterExpenseResultDto(
            expense.Id,
            cashSession?.Id,
            expense.Amount,
            cashSession?.ClosingBalance));
    }
}

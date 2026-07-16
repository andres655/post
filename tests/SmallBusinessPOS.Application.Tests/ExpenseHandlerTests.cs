using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Expenses.GetExpenses;
using SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class ExpenseHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static async Task<FixtureData> SeedFixtureAsync(IAppDbContext db)
    {
        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var register = CashRegister.Create(business.Id, branch.Id, "C01", "Caja principal");

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        await db.SaveChangesAsync();

        return new FixtureData(business.Id, branch.Id, register.Id);
    }

    [Fact]
    public async Task RegisterExpense_ShouldCreateExpenseWithoutCashMovement_WhenNotPaidFromCash()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new RegisterExpenseHandler(db, new RegisterExpenseValidator());

        var result = await handler.HandleAsync(new RegisterExpenseCommand(
            fixture.BusinessId,
            fixture.BranchId,
            "Operativo",
            "Compra menor",
            150m,
            PaidFromCash: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.CashSessionId.Should().BeNull();

        (await db.Expenses.CountAsync()).Should().Be(1);
        (await db.CashMovements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegisterExpense_ShouldFail_WhenPaidFromCashAndNoOpenSession()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new RegisterExpenseHandler(db, new RegisterExpenseValidator());

        var result = await handler.HandleAsync(new RegisterExpenseCommand(
            fixture.BusinessId,
            fixture.BranchId,
            "Operativo",
            "Compra de gas",
            500m,
            PaidFromCash: true));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Expense.CashSessionRequired");
        (await db.Expenses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegisterExpense_ShouldCreateCashMovement_AndReduceExpectedCash()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            1000m));

        var handler = new RegisterExpenseHandler(db, new RegisterExpenseValidator());
        var result = await handler.HandleAsync(new RegisterExpenseCommand(
            fixture.BusinessId,
            fixture.BranchId,
            "Combustible",
            "Compra de gas",
            300m,
            PaidFromCash: true), "cashier@pollosaboroso.local");

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpectedCash.Should().Be(700m);

        var session = await db.CashSessions.SingleAsync();
        session.TotalExpenses.Should().Be(300m);
        session.ClosingBalance.Should().Be(700m);

        var movement = await db.CashMovements.SingleAsync(m => m.MovementType == CashMovementType.Expense);
        movement.Amount.Should().Be(300m);
        movement.ReferenceType.Should().Be("Expense");
        movement.ReferenceId.Should().Be(result.Value.ExpenseId);
    }

    [Fact]
    public async Task GetExpenses_ShouldReturnExpensesInDateRange()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var register = new RegisterExpenseHandler(db, new RegisterExpenseValidator());
        await register.HandleAsync(new RegisterExpenseCommand(
            fixture.BusinessId,
            fixture.BranchId,
            "Operativo",
            "Compra menor",
            150m,
            PaidFromCash: false));

        var handler = new GetExpensesHandler(db);
        var result = await handler.HandleAsync(new GetExpensesQuery(
            fixture.BusinessId,
            fixture.BranchId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Amount.Should().Be(150m);
    }

    private sealed record FixtureData(Guid BusinessId, Guid BranchId, Guid RegisterId);
}

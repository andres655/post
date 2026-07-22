using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.CloseCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionHistory;
using SmallBusinessPOS.Application.Features.CashSessions.GetCashSessionSummary;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.CashSessions.RegisterCashWithdrawal;
using SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;
using SmallBusinessPOS.Application.Features.Sales.CancelSale;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class CashSessionSummaryHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetCashSessionSummary_ShouldReturnCashClosingBreakdown()
    {
        var db = CreateDb();

        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var register = CashRegister.Create(business.Id, branch.Id, "C01", "Caja principal");
        var cash = PaymentMethod.Create(business.Id, "CASH", "Efectivo", PaymentMethodType.Cash);
        var product = Product.Create(
            business.Id,
            "POL-ENT",
            "Pollo entero",
            ProductType.PreparedItem,
            UnitOfMeasure.Unit,
            650m,
            tracksInventory: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        db.PaymentMethods.Add(cash);
        db.Products.Add(product);
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 10m));
        db.BusinessSettings.Add(BusinessSettings.CreateDefault(business.Id));
        await db.SaveChangesAsync();

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(business.Id, branch.Id, register.Id, 500m));

        var createSale = new CreateSaleHandler(db, new CreateSaleValidator(), new TestClock());
        var sale = await createSale.HandleAsync(new CreateSaleCommand(
            business.Id,
            branch.Id,
            register.Id,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(product.Id, 1m, 650m)],
            [new CreateSalePayment(cash.Id, 650m)]));
        sale.IsSuccess.Should().BeTrue();

        var expense = new RegisterExpenseHandler(db, new RegisterExpenseValidator());
        var expenseResult = await expense.HandleAsync(new RegisterExpenseCommand(
            business.Id,
            branch.Id,
            "Operativo",
            "Compra menor",
            100m,
            PaidFromCash: true));
        expenseResult.IsSuccess.Should().BeTrue();

        var withdrawal = new RegisterCashWithdrawalHandler(db, new RegisterCashWithdrawalValidator());
        var withdrawalResult = await withdrawal.HandleAsync(new RegisterCashWithdrawalCommand(
            register.Id,
            50m,
            "Retiro parcial",
            "Entrega a supervisor"));
        withdrawalResult.IsSuccess.Should().BeTrue();

        var cancel = new CancelSaleHandler(db, new CancelSaleValidator(), new TestClock());
        var cancelResult = await cancel.HandleAsync(new CancelSaleCommand(sale.Value.SaleId, "Error"));
        cancelResult.IsSuccess.Should().BeTrue();

        var handler = new GetCashSessionSummaryHandler(db);
        var result = await handler.HandleAsync(new GetCashSessionSummaryQuery(register.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OpeningAmount.Should().Be(500m);
        result.Value.CashSales.Should().Be(650m);
        result.Value.Expenses.Should().Be(100m);
        result.Value.Refunds.Should().Be(650m);
        result.Value.Withdrawals.Should().Be(50m);
        result.Value.ExpectedCash.Should().Be(350m);
    }

    [Fact]
    public async Task GetCashSessionHistory_ShouldReturnClosedSessionsWithBreakdown()
    {
        var db = CreateDb();

        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var register = CashRegister.Create(business.Id, branch.Id, "C01", "Caja principal");

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        await db.SaveChangesAsync();

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        var opened = await open.HandleAsync(new OpenCashSessionCommand(business.Id, branch.Id, register.Id, 500m));
        opened.IsSuccess.Should().BeTrue();

        var withdrawal = new RegisterCashWithdrawalHandler(db, new RegisterCashWithdrawalValidator());
        var withdrawalResult = await withdrawal.HandleAsync(new RegisterCashWithdrawalCommand(
            register.Id,
            125m,
            "Deposito bancario",
            null));
        withdrawalResult.IsSuccess.Should().BeTrue();

        var close = new CloseCashSessionHandler(db, new CloseCashSessionValidator());
        var closeResult = await close.HandleAsync(new CloseCashSessionCommand(opened.Value.Id, 375m, "Cierre exacto"));
        closeResult.IsSuccess.Should().BeTrue();

        var history = new GetCashSessionHistoryHandler(db);
        var result = await history.HandleAsync(new GetCashSessionHistoryQuery(register.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var session = result.Value.Single();
        session.OpeningAmount.Should().Be(500m);
        session.Withdrawals.Should().Be(125m);
        session.ExpectedCash.Should().Be(375m);
        session.CountedCash.Should().Be(375m);
        session.Difference.Should().Be(0m);
    }
}

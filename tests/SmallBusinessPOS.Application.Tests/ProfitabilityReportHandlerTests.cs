using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;
using SmallBusinessPOS.Application.Features.Reports.GetProfitabilityReport;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class ProfitabilityReportHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetProfitabilityReport_ShouldReturnRangeProfitability()
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
            estimatedCost: 280m,
            tracksInventory: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        db.PaymentMethods.Add(cash);
        db.Products.Add(product);
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 20m));
        db.BusinessSettings.Add(BusinessSettings.CreateDefault(business.Id));
        await db.SaveChangesAsync();

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(business.Id, branch.Id, register.Id, 0m));

        var create = new CreateSaleHandler(db, new CreateSaleValidator());
        var sale = await create.HandleAsync(new CreateSaleCommand(
            business.Id,
            branch.Id,
            register.Id,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(product.Id, 2m, 650m)],
            [new CreateSalePayment(cash.Id, 1300m)]));
        sale.IsSuccess.Should().BeTrue();

        var expense = new RegisterExpenseHandler(db, new RegisterExpenseValidator());
        var expenseResult = await expense.HandleAsync(new RegisterExpenseCommand(
            business.Id,
            branch.Id,
            "Operativo",
            "Carbón",
            300m,
            PaidFromCash: true));
        expenseResult.IsSuccess.Should().BeTrue();

        var handler = new GetProfitabilityReportHandler(db);
        var result = await handler.HandleAsync(new GetProfitabilityReportQuery(
            business.Id,
            branch.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow)));

        result.IsSuccess.Should().BeTrue();
        result.Value.GrossSales.Should().Be(1300m);
        result.Value.NetSales.Should().Be(1300m);
        result.Value.EstimatedCost.Should().Be(560m);
        result.Value.GrossMargin.Should().Be(740m);
        result.Value.Expenses.Should().Be(300m);
        result.Value.NetProfit.Should().Be(440m);
        result.Value.Products.Should().ContainSingle();
        result.Value.Products[0].GrossMarginPercent.Should().BeApproximately(56.923m, 0.001m);
        result.Value.Daily.Should().ContainSingle();
        result.Value.Daily[0].NetProfit.Should().Be(440m);
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Expenses.RegisterExpense;
using SmallBusinessPOS.Application.Features.Production.ConfirmProductionEntry;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Features.Sales.GetDailyReport;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class DailyReportHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetDailyReport_ShouldReturnRealAggregates()
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
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 10m));
        db.BusinessSettings.Add(BusinessSettings.CreateDefault(business.Id));
        await db.SaveChangesAsync();

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(business.Id, branch.Id, register.Id, 0m));

        var production = new ConfirmProductionEntryHandler(db, new ConfirmProductionEntryValidator());
        await production.HandleAsync(new ConfirmProductionEntryCommand(
            null,
            business.Id,
            branch.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new ConfirmProductionEntryLine(product.Id, 40m, 280m, QuantityWasted: 1m)]));

        var create = new CreateSaleHandler(db, new CreateSaleValidator());
        await create.HandleAsync(new CreateSaleCommand(
            business.Id,
            branch.Id,
            register.Id,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(product.Id, 2m, 650m)],
            [new CreateSalePayment(cash.Id, 1300m)]));

        var expense = new RegisterExpenseHandler(db, new RegisterExpenseValidator());
        await expense.HandleAsync(new RegisterExpenseCommand(
            business.Id,
            branch.Id,
            "Operativo",
            "Compra de gas",
            300m,
            PaidFromCash: true));

        var handler = new GetDailyReportHandler(db);
        var report = await handler.HandleAsync(new GetDailyReportQuery(
            business.Id,
            branch.Id,
            DateOnly.FromDateTime(DateTime.UtcNow)));

        report.IsSuccess.Should().BeTrue();
        report.Value.GrossSales.Should().Be(1300m);
        report.Value.SalesCount.Should().Be(1);
        report.Value.SalesByPaymentMethod.Should().ContainSingle();
        report.Value.Expenses.Should().Be(300m);
        report.Value.PollosPrepared.Should().Be(40m);
        report.Value.PollosSoldEquivalent.Should().Be(2m);
        report.Value.PollosAvailable.Should().Be(47m);
        report.Value.Waste.Should().Be(1m);
        report.Value.TopProducts.Should().ContainSingle();
        report.Value.TopProducts[0].EstimatedCost.Should().Be(560m);
        report.Value.TopProducts[0].GrossMargin.Should().Be(740m);
        report.Value.TopProducts[0].GrossMarginPercent.Should().BeApproximately(56.923m, 0.001m);
    }
}

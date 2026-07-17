using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Reports.GetManagementDashboard;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class ManagementDashboardHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetManagementDashboard_ShouldReturnKpisAndRecentActivity()
    {
        var db = CreateDb();

        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var register = CashRegister.Create(business.Id, branch.Id, "C01", "Caja principal");
        var cash = PaymentMethod.Create(business.Id, "CASH", "Efectivo", PaymentMethodType.Cash);
        var product = Product.Create(business.Id, "POL-ENT", "Pollo entero", ProductType.PreparedItem, UnitOfMeasure.Unit, 650m, estimatedCost: 280m, tracksInventory: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        db.PaymentMethods.Add(cash);
        db.Products.Add(product);
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 10m));
        db.BusinessSettings.Add(BusinessSettings.CreateDefault(business.Id));
        await db.SaveChangesAsync();

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(business.Id, branch.Id, register.Id, 100m));

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

        var handler = new GetManagementDashboardHandler(db);
        var result = await handler.HandleAsync(new GetManagementDashboardQuery(
            business.Id,
            branch.Id,
            DateOnly.FromDateTime(DateTime.UtcNow)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Kpis.TodaySales.Should().Be(1300m);
        result.Value.Kpis.TodaySalesCount.Should().Be(1);
        result.Value.Kpis.TodayGrossMargin.Should().Be(740m);
        result.Value.Kpis.ExpectedCash.Should().Be(1400m);
        result.Value.Kpis.HasOpenCashSession.Should().BeTrue();
        result.Value.TopProducts.Should().ContainSingle();
        result.Value.RecentActivity.Should().NotBeEmpty();
    }
}

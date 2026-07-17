using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Reports.GetOperationalAudit;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class OperationalAuditHandlerTests
{
    private static IAppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task GetOperationalAudit_ShouldReturnTimelineFilteredByUser()
    {
        var db = CreateDb();

        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var register = CashRegister.Create(business.Id, branch.Id, "C01", "Caja principal");
        var cash = PaymentMethod.Create(business.Id, "CASH", "Efectivo", PaymentMethodType.Cash);
        var product = Product.Create(business.Id, "POL-ENT", "Pollo entero", ProductType.PreparedItem, UnitOfMeasure.Unit, 650m, tracksInventory: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        db.PaymentMethods.Add(cash);
        db.Products.Add(product);
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 10m));
        db.BusinessSettings.Add(BusinessSettings.CreateDefault(business.Id));
        await db.SaveChangesAsync();

        var actor = "cajero@pollosaboroso.local";
        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(business.Id, branch.Id, register.Id, 0m), actor);

        var create = new CreateSaleHandler(db, new CreateSaleValidator());
        var sale = await create.HandleAsync(new CreateSaleCommand(
            business.Id,
            branch.Id,
            register.Id,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(product.Id, 1m, 650m)],
            [new CreateSalePayment(cash.Id, 650m)]), actor);
        sale.IsSuccess.Should().BeTrue();

        var handler = new GetOperationalAuditHandler(db);
        var result = await handler.HandleAsync(new GetOperationalAuditQuery(
            business.Id,
            branch.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow),
            User: "cajero"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().OnlyContain(e => e.User.Contains("cajero", StringComparison.OrdinalIgnoreCase));
        result.Value.Should().Contain(e => e.Area == "Ventas" && e.Action.Contains("Venta", StringComparison.OrdinalIgnoreCase));
        result.Value.Should().Contain(e => e.Area == "Caja");
        result.Value.Should().Contain(e => e.Area == "Inventario");
    }
}

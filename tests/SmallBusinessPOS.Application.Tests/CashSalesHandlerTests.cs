using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.Sales.CancelSale;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public class CashSalesHandlerTests
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

        var cash = PaymentMethod.Create(business.Id, "CASH", "Efectivo", PaymentMethodType.Cash);
        var card = PaymentMethod.Create(business.Id, "CARD", "Tarjeta", PaymentMethodType.DebitCard);

        var polloEntero = Product.Create(business.Id, "POL-ENT", "Pollo entero", ProductType.PreparedItem, UnitOfMeasure.Unit, 650m, tracksInventory: true);
        var medioPollo = Product.Create(business.Id, "POL-MED", "Medio pollo", ProductType.PreparedItem, UnitOfMeasure.Portion, 350m, tracksInventory: false);
        var yuca = Product.Create(business.Id, "YUC-GRD", "Yuca grande", ProductType.PreparedItem, UnitOfMeasure.Portion, 120m, tracksInventory: false);
        var combo = Product.Create(business.Id, "CMB-FAM", "Combo familiar", ProductType.Combo, UnitOfMeasure.Unit, 1000m, tracksInventory: false);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        db.PaymentMethods.AddRange(cash, card);
        db.Products.AddRange(polloEntero, medioPollo, yuca, combo);

        db.ProductComponents.Add(ProductComponent.Create(medioPollo.Id, polloEntero.Id, 0.5m));
        db.ProductComponents.AddRange(
            ProductComponent.Create(combo.Id, polloEntero.Id, 1m),
            ProductComponent.Create(combo.Id, yuca.Id, 1m));

        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, polloEntero.Id, 20m));

        await db.SaveChangesAsync();

        return new FixtureData(
            business.Id,
            branch.Id,
            register.Id,
            cash.Id,
            card.Id,
            polloEntero.Id,
            medioPollo.Id,
            combo.Id);
    }

    [Fact]
    public async Task OpenCashSession_ShouldOpen_WhenRegisterIsFree()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new OpenCashSessionHandler(db, new OpenCashSessionValidator());

        var result = await handler.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            500m));

        result.IsSuccess.Should().BeTrue();
        result.Value.OpeningBalance.Should().Be(500m);
    }

    [Fact]
    public async Task OpenCashSession_ShouldFail_WhenAlreadyOpen()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new OpenCashSessionHandler(db, new OpenCashSessionValidator());

        await handler.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            100m));

        var second = await handler.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            200m));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("CashSession.AlreadyOpen");
    }

    [Fact]
    public async Task CreateSale_ShouldFail_WhenCashSessionClosed()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);
        var handler = new CreateSaleHandler(db, new CreateSaleValidator());

        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(fixture.PolloEnteroProductId, 1m, 650m)],
            [new CreateSalePayment(fixture.CashPaymentMethodId, 650m)]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.CashSessionRequired");
    }

    [Fact]
    public async Task CreateSale_ShouldDiscountInventory_AndGenerateOutbox()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(fixture.BusinessId, fixture.BranchId, fixture.RegisterId, 0m));

        var handler = new CreateSaleHandler(db, new CreateSaleValidator());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(fixture.PolloEnteroProductId, 2m, 650m)],
            [new CreateSalePayment(fixture.CashPaymentMethodId, 1300m)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().MatchRegex("^PRIN-C01-\\d{8}-\\d{6}$");

        var stock = await db.InventoryStocks.FirstAsync(s => s.ProductId == fixture.PolloEnteroProductId);
        stock.Quantity.Should().Be(18m);

        var outboxCount = await db.OutboxMessages.CountAsync();
        outboxCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateSale_ShouldKeepTenderedCashAndReturnChange()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(fixture.BusinessId, fixture.BranchId, fixture.RegisterId, 0m));

        var handler = new CreateSaleHandler(db, new CreateSaleValidator());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(fixture.PolloEnteroProductId, 1m, 650m)],
            [new CreateSalePayment(fixture.CashPaymentMethodId, 650m, TenderedAmount: 1000m)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Paid.Should().Be(650m);
        result.Value.Change.Should().Be(350m);

        var payment = await db.SalePayments.SingleAsync(p => p.SaleId == result.Value.SaleId);
        payment.Amount.Should().Be(650m);
        payment.TenderedAmount.Should().Be(1000m);

        var session = await db.CashSessions.SingleAsync();
        session.ClosingBalance.Should().Be(650m);
    }

    [Fact]
    public async Task CreateSale_ShouldConsumeComboComponents()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(fixture.BusinessId, fixture.BranchId, fixture.RegisterId, 0m));

        var handler = new CreateSaleHandler(db, new CreateSaleValidator());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(fixture.ComboProductId, 1m, 1000m)],
            [new CreateSalePayment(fixture.CashPaymentMethodId, 1000m)]));

        result.IsSuccess.Should().BeTrue();

        var stock = await db.InventoryStocks.FirstAsync(s => s.ProductId == fixture.PolloEnteroProductId);
        stock.Quantity.Should().Be(19m);
    }

    [Fact]
    public async Task CreateSale_ShouldConsumeHalfChickenAsPointFive()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(fixture.BusinessId, fixture.BranchId, fixture.RegisterId, 0m));

        var handler = new CreateSaleHandler(db, new CreateSaleValidator());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(fixture.MedioPolloProductId, 2m, 350m)],
            [new CreateSalePayment(fixture.CashPaymentMethodId, 700m)]));

        result.IsSuccess.Should().BeTrue();

        var stock = await db.InventoryStocks.FirstAsync(s => s.ProductId == fixture.PolloEnteroProductId);
        stock.Quantity.Should().Be(19m);
    }

    [Fact]
    public async Task CancelSale_ShouldRevertInventoryAndCash()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(fixture.BusinessId, fixture.BranchId, fixture.RegisterId, 100m));

        var create = new CreateSaleHandler(db, new CreateSaleValidator());
        var sale = await create.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(fixture.PolloEnteroProductId, 2m, 650m)],
            [new CreateSalePayment(fixture.CashPaymentMethodId, 1300m)]));

        sale.IsSuccess.Should().BeTrue();

        var cancel = new CancelSaleHandler(db, new CancelSaleValidator());
        var result = await cancel.HandleAsync(new CancelSaleCommand(sale.Value.SaleId, "Error de digitación"));

        result.IsSuccess.Should().BeTrue();

        var stock = await db.InventoryStocks.FirstAsync(s => s.ProductId == fixture.PolloEnteroProductId);
        stock.Quantity.Should().Be(20m);

        var movementTypes = await db.CashMovements
            .Where(m => m.ReferenceId == sale.Value.SaleId)
            .Select(m => m.MovementType)
            .ToListAsync();

        movementTypes.Should().Contain(CashMovementType.SaleIncome);
        movementTypes.Should().Contain(CashMovementType.Refund);
    }

    [Fact]
    public async Task CancelSale_ShouldFail_WhenAlreadyCancelled()
    {
        var db = CreateDb();
        var fixture = await SeedFixtureAsync(db);

        var open = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        await open.HandleAsync(new OpenCashSessionCommand(fixture.BusinessId, fixture.BranchId, fixture.RegisterId, 100m));

        var create = new CreateSaleHandler(db, new CreateSaleValidator());
        var sale = await create.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            0m,
            0m,
            [new CreateSaleLine(fixture.PolloEnteroProductId, 1m, 650m)],
            [new CreateSalePayment(fixture.CashPaymentMethodId, 650m)]));

        var cancel = new CancelSaleHandler(db, new CancelSaleValidator());
        await cancel.HandleAsync(new CancelSaleCommand(sale.Value.SaleId, "Error"));
        var second = await cancel.HandleAsync(new CancelSaleCommand(sale.Value.SaleId, "Duplicado"));

        second.IsFailure.Should().BeTrue();
    }

    private sealed record FixtureData(
        Guid BusinessId,
        Guid BranchId,
        Guid RegisterId,
        Guid CashPaymentMethodId,
        Guid CardPaymentMethodId,
        Guid PolloEnteroProductId,
        Guid MedioPolloProductId,
        Guid ComboProductId);
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Features.POS.GetPosContext;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Application.Features.Sales.RegisterSaleReturn;
using SmallBusinessPOS.Application.Features.Settings.GetBusinessSettings;
using SmallBusinessPOS.Application.Features.Settings.UpdateBusinessSettings;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Application.Tests;

public sealed class ApplicationPosFlowTests
{
    private static TestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task CreateSale_ShouldCalculateSaleTotals_FromServerValues()
    {
        await using var db = CreateDb();
        var fixture = await SeedAsync(db);
        await OpenRegisterAsync(db, fixture);

        var handler = new CreateSaleHandler(db, new CreateSaleValidator(), new TestClock());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            Discount: 50m,
            Tax: 999m,
            Lines: [new CreateSaleLine(fixture.ProductId, 2m, 1m)],
            Payments: [new CreateSalePayment(fixture.CashMethodId, 1250m)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Subtotal.Should().Be(1300m);
        result.Value.Discount.Should().Be(50m);
        result.Value.Tax.Should().Be(0m);
        result.Value.Total.Should().Be(1250m);

        var detail = await db.SaleDetails.SingleAsync(item => item.SaleId == result.Value.SaleId);
        detail.UnitPrice.Should().Be(650m);
    }

    [Fact]
    public async Task CreateSale_ShouldSupportMixedPayments_AndCalculateCashChangeOnly()
    {
        await using var db = CreateDb();
        var fixture = await SeedAsync(db);
        await OpenRegisterAsync(db, fixture);

        var handler = new CreateSaleHandler(db, new CreateSaleValidator(), new TestClock());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            Discount: 0m,
            Tax: 0m,
            Lines: [new CreateSaleLine(fixture.ProductId, 1m, 650m)],
            Payments:
            [
                new CreateSalePayment(fixture.CashMethodId, 250m, TenderedAmount: 300m),
                new CreateSalePayment(fixture.CardMethodId, 400m, Reference: "AUTH-123")
            ]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Paid.Should().Be(650m);
        result.Value.Change.Should().Be(50m);

        var session = await db.CashSessions.SingleAsync(item => item.CashRegisterId == fixture.RegisterId);
        session.ClosingBalance.Should().Be(250m);

        var payments = await db.SalePayments.Where(item => item.SaleId == result.Value.SaleId).ToListAsync();
        payments.Should().Contain(item => item.PaymentMethodId == fixture.CashMethodId && item.TenderedAmount == 300m);
        payments.Should().Contain(item => item.PaymentMethodId == fixture.CardMethodId && item.Reference == "AUTH-123");
    }

    [Fact]
    public async Task CreateSale_ShouldCalculateTaxes_FromBusinessSettings()
    {
        await using var db = CreateDb();
        var fixture = await SeedAsync(db, usesTaxes: true, defaultTaxRate: 18m);
        await OpenRegisterAsync(db, fixture);

        var handler = new CreateSaleHandler(db, new CreateSaleValidator(), new TestClock());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            Discount: 50m,
            Tax: 0m,
            Lines: [new CreateSaleLine(fixture.ProductId, 1m, 650m)],
            Payments: [new CreateSalePayment(fixture.CashMethodId, 708m)]));

        result.IsSuccess.Should().BeTrue();
        result.Value.Subtotal.Should().Be(650m);
        result.Value.Discount.Should().Be(50m);
        result.Value.Tax.Should().Be(108m);
        result.Value.Total.Should().Be(708m);
    }

    [Fact]
    public async Task CreateSale_ShouldAllowCredit_WhenEnabledAndCustomerSelected()
    {
        await using var db = CreateDb();
        var fixture = await SeedAsync(db, allowsCredit: true);
        await OpenRegisterAsync(db, fixture);

        var handler = new CreateSaleHandler(db, new CreateSaleValidator(), new TestClock());
        var result = await handler.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            Discount: 0m,
            Tax: 0m,
            Lines: [new CreateSaleLine(fixture.ProductId, 1m, 650m)],
            Payments: [new CreateSalePayment(fixture.CreditMethodId, 650m, Reference: "CRED-001")],
            CustomerId: fixture.CustomerId));

        result.IsSuccess.Should().BeTrue();

        var sale = await db.Sales.SingleAsync(item => item.Id == result.Value.SaleId);
        sale.CustomerId.Should().Be(fixture.CustomerId);

        var session = await db.CashSessions.SingleAsync(item => item.CashRegisterId == fixture.RegisterId);
        session.ClosingBalance.Should().Be(0m);
    }

    [Fact]
    public async Task RegisterSaleReturn_ShouldRefundPartialQuantity_UpdateStockCashAndSaleStatus()
    {
        await using var db = CreateDb();
        var fixture = await SeedAsync(db);
        await OpenRegisterAsync(db, fixture);

        var create = new CreateSaleHandler(db, new CreateSaleValidator(), new TestClock());
        var saleResult = await create.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            Discount: 0m,
            Tax: 0m,
            Lines: [new CreateSaleLine(fixture.ProductId, 2m, 650m)],
            Payments: [new CreateSalePayment(fixture.CashMethodId, 1300m)]));
        saleResult.IsSuccess.Should().BeTrue();

        var saleDetailId = await db.SaleDetails
            .Where(item => item.SaleId == saleResult.Value.SaleId)
            .Select(item => item.Id)
            .SingleAsync();

        var handler = new RegisterSaleReturnHandler(db, new RegisterSaleReturnValidator());
        var returnResult = await handler.HandleAsync(new RegisterSaleReturnCommand(
            saleResult.Value.SaleId,
            fixture.CashMethodId,
            "Cliente devuelve una unidad",
            [new RegisterSaleReturnLine(saleDetailId, 1m)]));

        returnResult.IsSuccess.Should().BeTrue();

        var sale = await db.Sales.SingleAsync(item => item.Id == saleResult.Value.SaleId);
        sale.Status.Should().Be(SaleStatus.PartiallyRefunded);

        var saleReturn = await db.SaleReturns.Include(item => item.Details).SingleAsync();
        saleReturn.Total.Should().Be(650m);
        saleReturn.Details.Should().ContainSingle(item => item.Quantity == 1m);

        var stock = await db.InventoryStocks.SingleAsync(item => item.ProductId == fixture.ProductId);
        stock.Quantity.Should().Be(9m);

        var session = await db.CashSessions.SingleAsync(item => item.CashRegisterId == fixture.RegisterId);
        session.ClosingBalance.Should().Be(650m);
    }

    [Fact]
    public async Task GetPosContext_ShouldSelectRequestedBranchAndRegister()
    {
        await using var db = CreateDb();
        var fixture = await SeedAsync(db);
        await OpenRegisterAsync(db, fixture, fixture.SecondRegisterId);

        var handler = new GetPosContextHandler(db);
        var result = await handler.HandleAsync(new GetPosContextQuery(
            fixture.BusinessId,
            fixture.SecondBranchId,
            fixture.SecondRegisterId));

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchId.Should().Be(fixture.SecondBranchId);
        result.Value.BranchName.Should().Be("Sucursal Norte");
        result.Value.CashRegisterId.Should().Be(fixture.SecondRegisterId);
        result.Value.CashRegisterCode.Should().Be("N01");
        result.Value.HasOpenCashSession.Should().BeTrue();
    }

    [Fact]
    public async Task SettingsHandlers_ShouldCreateUpdateAndReadBusinessSettings()
    {
        await using var db = CreateDb();
        var fixture = await SeedAsync(db, addSettings: false);

        var update = new UpdateBusinessSettingsHandler(db, new UpdateBusinessSettingsValidator());
        var updateResult = await update.HandleAsync(new UpdateBusinessSettingsCommand(
            fixture.BusinessId,
            fixture.BranchId,
            "  Pollo Norte  ",
            " RNC-01 ",
            " 809-555-0101 ",
            " Calle 1 ",
            " DOP ",
            " Principal Renovada ",
            " 809-555-0202 ",
            " Ave Central ",
            UsesInventory: true,
            UsesProduction: true,
            UsesKitchen: false,
            UsesDelivery: true,
            UsesCustomers: true,
            UsesTaxes: true,
            AllowsCredit: true,
            AllowsNegativeInventory: false,
            CurrencySymbol: "RD$",
            DefaultTaxRate: 18.126m,
            ReceiptLogoPath: " uploads/logos/logo.webp ",
            ReceiptHeader: " Gracias ",
            TicketFooter: " Vuelva pronto "));

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value.BusinessName.Should().Be("Pollo Norte");
        updateResult.Value.BranchName.Should().Be("Principal Renovada");
        updateResult.Value.UsesTaxes.Should().BeTrue();
        updateResult.Value.AllowsCredit.Should().BeTrue();
        updateResult.Value.DefaultTaxRate.Should().Be(18.13m);

        var get = new GetBusinessSettingsHandler(db);
        var getResult = await get.HandleAsync(new GetBusinessSettingsQuery(fixture.BusinessId, fixture.BranchId));

        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.BusinessName.Should().Be("Pollo Norte");
        getResult.Value.TaxId.Should().Be("RNC-01");
        getResult.Value.ReceiptLogoPath.Should().Be("uploads/logos/logo.webp");
    }

    private static async Task OpenRegisterAsync(TestDbContext db, Fixture fixture, Guid? registerId = null)
    {
        var handler = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        var targetRegisterId = registerId ?? fixture.RegisterId;
        var branchId = targetRegisterId == fixture.SecondRegisterId ? fixture.SecondBranchId : fixture.BranchId;

        var result = await handler.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            branchId,
            targetRegisterId,
            0m));

        result.IsSuccess.Should().BeTrue();
    }

    private static async Task<Fixture> SeedAsync(
        IAppDbContext db,
        bool addSettings = true,
        bool usesTaxes = false,
        decimal defaultTaxRate = 0m,
        bool allowsCredit = false)
    {
        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Principal", isMain: true);
        var secondBranch = Branch.Create(business.Id, "Sucursal Norte", isMain: false);
        var register = CashRegister.Create(business.Id, branch.Id, "C01", "Caja principal");
        var secondRegister = CashRegister.Create(business.Id, secondBranch.Id, "N01", "Caja norte");
        var cash = PaymentMethod.Create(business.Id, "CASH", "Efectivo", PaymentMethodType.Cash);
        var card = PaymentMethod.Create(business.Id, "CARD", "Tarjeta", PaymentMethodType.DebitCard);
        var credit = PaymentMethod.Create(business.Id, "CREDIT", "Credito", PaymentMethodType.Credit);
        var product = Product.Create(
            business.Id,
            "POL-ENT",
            "Pollo entero",
            ProductType.PreparedItem,
            UnitOfMeasure.Unit,
            650m,
            tracksInventory: true);
        var customer = Customer.Create(business.Id, "Cliente de credito");

        db.Businesses.Add(business);
        db.Branches.AddRange(branch, secondBranch);
        db.CashRegisters.AddRange(register, secondRegister);
        db.PaymentMethods.AddRange(cash, card, credit);
        db.Products.Add(product);
        db.Customers.Add(customer);
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 10m));

        if (addSettings)
        {
            var settings = BusinessSettings.CreateDefault(business.Id);
            settings.Update(
                usesInventory: true,
                usesProduction: true,
                usesKitchen: false,
                usesDelivery: false,
                usesCustomers: true,
                usesTaxes,
                allowsCredit,
                allowsNegativeInventory: false,
                currencySymbol: "RD$",
                defaultTaxRate,
                receiptLogoPath: null,
                receiptHeader: null,
                ticketFooter: null);
            db.BusinessSettings.Add(settings);
        }

        await db.SaveChangesAsync();

        return new Fixture(
            business.Id,
            branch.Id,
            secondBranch.Id,
            register.Id,
            secondRegister.Id,
            cash.Id,
            card.Id,
            credit.Id,
            product.Id,
            customer.Id);
    }

    private sealed record Fixture(
        Guid BusinessId,
        Guid BranchId,
        Guid SecondBranchId,
        Guid RegisterId,
        Guid SecondRegisterId,
        Guid CashMethodId,
        Guid CardMethodId,
        Guid CreditMethodId,
        Guid ProductId,
        Guid CustomerId);
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Features.CashSessions.OpenCashSession;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Application.Features.Sales.CancelSale;
using SmallBusinessPOS.Application.Features.Sales.CreateSale;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.IntegrationTests;

public sealed class CompleteSaleIntegrationTests
{
    [Fact]
    public async Task CreateSale_ShouldPersistSalePaymentsInventoryCashAndOutbox()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SaleIntegrationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SaleIntegrationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var fixture = await SeedFixtureAsync(db);

        var openCash = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        var openResult = await openCash.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            500m), "cashier@pollosaboroso.local");

        Assert.True(openResult.IsSuccess);

        var createSale = new CreateSaleHandler(db, new CreateSaleValidator());
        var saleResult = await createSale.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            Discount: 50m,
            Tax: 0m,
            Lines:
            [
                new CreateSaleLine(fixture.ProductId, 2m, 650m)
            ],
            Payments:
            [
                new CreateSalePayment(fixture.CashPaymentMethodId, 500m),
                new CreateSalePayment(fixture.CardPaymentMethodId, 750m, "AUTH-123")
            ],
            DeviceId: "TEST-POS-01"), "cashier@pollosaboroso.local");

        Assert.True(saleResult.IsSuccess);

        var saleId = saleResult.Value.SaleId;

        await using var verification = new SaleIntegrationDbContext(options);

        var sale = await verification.Sales
            .Include(s => s.Details)
            .Include(s => s.Payments)
            .SingleAsync(s => s.Id == saleId);

        Assert.Equal(SaleStatus.Confirmed, sale.Status);
        Assert.Equal(1250m, sale.Total);
        Assert.Single(sale.Details);
        Assert.Equal(2, sale.Payments.Count);
        Assert.Equal(1250m, sale.Payments.Sum(p => p.Amount));

        var stock = await verification.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        Assert.Equal(8m, stock.Quantity);

        var inventoryMovement = await verification.InventoryMovements.SingleAsync(m =>
            m.ReferenceType == "Sale" && m.ReferenceId == saleId && m.MovementType == MovementType.Sale);
        Assert.Equal(2m, inventoryMovement.Quantity);
        Assert.Equal(10m, inventoryMovement.PreviousQuantity);
        Assert.Equal(8m, inventoryMovement.NewQuantity);

        var cashMovement = await verification.CashMovements.SingleAsync(m =>
            m.ReferenceType == "Sale" && m.ReferenceId == saleId && m.MovementType == CashMovementType.SaleIncome);
        Assert.Equal(500m, cashMovement.Amount);

        var cashSession = await verification.CashSessions.SingleAsync(s => s.Id == openResult.Value.Id);
        Assert.Equal(500m, cashSession.OpeningBalance);
        Assert.Equal(500m, cashSession.TotalIncome);
        Assert.Equal(1000m, cashSession.ClosingBalance);

        var outbox = await verification.OutboxMessages.SingleAsync(m => m.AggregateId == saleId);
        Assert.Equal("SaleConfirmed", outbox.EventType);
        Assert.Contains(sale.ReceiptNumber, outbox.Payload);

        var sequence = await verification.SaleNumberSequences.SingleAsync(s => s.CashRegisterId == fixture.RegisterId);
        Assert.Equal(1, sequence.LastSequence);
    }

    [Fact]
    public async Task CreateSale_InsideTransaction_ShouldRollbackAllSaleSideEffects()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SaleIntegrationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SaleIntegrationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var fixture = await SeedFixtureAsync(db);

        var openCash = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        var openResult = await openCash.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            500m));

        Assert.True(openResult.IsSuccess);

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var createSale = new CreateSaleHandler(db, new CreateSaleValidator());
            var saleResult = await createSale.HandleAsync(new CreateSaleCommand(
                fixture.BusinessId,
                fixture.BranchId,
                fixture.RegisterId,
                SaleType.Counter,
                Discount: 0m,
                Tax: 0m,
                Lines:
                [
                    new CreateSaleLine(fixture.ProductId, 1m, 650m)
                ],
                Payments:
                [
                    new CreateSalePayment(fixture.CashPaymentMethodId, 650m)
                ]));

            Assert.True(saleResult.IsSuccess);
            await transaction.RollbackAsync();
        }

        await using var verification = new SaleIntegrationDbContext(options);

        Assert.Empty(await verification.Sales.ToListAsync());
        Assert.Empty(await verification.SalePayments.ToListAsync());
        Assert.Empty(await verification.InventoryMovements.ToListAsync());
        Assert.Empty(await verification.CashMovements.Where(m => m.MovementType == CashMovementType.SaleIncome).ToListAsync());
        Assert.Empty(await verification.OutboxMessages.ToListAsync());

        var stock = await verification.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        Assert.Equal(10m, stock.Quantity);

        var cashSession = await verification.CashSessions.SingleAsync(s => s.Id == openResult.Value.Id);
        Assert.Equal(500m, cashSession.ClosingBalance);
        Assert.Equal(0m, cashSession.TotalIncome);
    }

    [Fact]
    public async Task CancelSale_ShouldRevertInventoryRegisterRefundAndKeepSaleSnapshot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SaleIntegrationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SaleIntegrationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var fixture = await SeedFixtureAsync(db);

        var openCash = new OpenCashSessionHandler(db, new OpenCashSessionValidator());
        var openResult = await openCash.HandleAsync(new OpenCashSessionCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            500m), "cashier@pollosaboroso.local");
        Assert.True(openResult.IsSuccess);

        var createSale = new CreateSaleHandler(db, new CreateSaleValidator());
        var saleResult = await createSale.HandleAsync(new CreateSaleCommand(
            fixture.BusinessId,
            fixture.BranchId,
            fixture.RegisterId,
            SaleType.Counter,
            Discount: 0m,
            Tax: 0m,
            Lines:
            [
                new CreateSaleLine(fixture.ProductId, 2m, 650m)
            ],
            Payments:
            [
                new CreateSalePayment(fixture.CashPaymentMethodId, 1300m)
            ],
            DeviceId: "TEST-POS-01"), "cashier@pollosaboroso.local");
        Assert.True(saleResult.IsSuccess);

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var cancelSale = new CancelSaleHandler(db, new CancelSaleValidator());
            var cancelResult = await cancelSale.HandleAsync(new CancelSaleCommand(
                saleResult.Value.SaleId,
                "Error de digitacion",
                "TEST-POS-01"), "supervisor@pollosaboroso.local");

            Assert.True(cancelResult.IsSuccess);
            await transaction.CommitAsync();
        }

        await using var verification = new SaleIntegrationDbContext(options);

        var sale = await verification.Sales
            .Include(s => s.Details)
            .Include(s => s.Payments)
            .SingleAsync(s => s.Id == saleResult.Value.SaleId);

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.Equal("Error de digitacion", sale.CancellationReason);
        Assert.Equal("supervisor@pollosaboroso.local", sale.CancelledBy);
        Assert.Equal(saleResult.Value.Number, sale.ReceiptNumber);
        Assert.Equal(1300m, sale.Total);
        Assert.Equal("POL-ENT", sale.Details.Single().ProductCode);
        Assert.Equal("Pollo horneado entero", sale.Details.Single().ProductName);
        Assert.Equal(650m, sale.Details.Single().UnitPrice);
        Assert.Equal(1300m, sale.Payments.Sum(p => p.Amount));

        var stock = await verification.InventoryStocks.SingleAsync(s => s.ProductId == fixture.ProductId);
        Assert.Equal(10m, stock.Quantity);

        var saleMovement = await verification.InventoryMovements.SingleAsync(m =>
            m.ReferenceType == "Sale" && m.ReferenceId == sale.Id && m.MovementType == MovementType.Sale);
        Assert.Equal(2m, saleMovement.Quantity);
        Assert.Equal(10m, saleMovement.PreviousQuantity);
        Assert.Equal(8m, saleMovement.NewQuantity);

        var cancellationMovement = await verification.InventoryMovements.SingleAsync(m =>
            m.ReferenceType == "SaleCancellation"
            && m.ReferenceId == sale.Id
            && m.MovementType == MovementType.SaleCancellation);
        Assert.Equal(2m, cancellationMovement.Quantity);
        Assert.Equal(8m, cancellationMovement.PreviousQuantity);
        Assert.Equal(10m, cancellationMovement.NewQuantity);

        var refund = await verification.CashMovements.SingleAsync(m =>
            m.ReferenceType == "SaleCancellation"
            && m.ReferenceId == sale.Id
            && m.MovementType == CashMovementType.Refund);
        Assert.Equal(1300m, refund.Amount);

        var cashSession = await verification.CashSessions.SingleAsync(s => s.Id == openResult.Value.Id);
        Assert.Equal(1300m, cashSession.TotalIncome);
        Assert.Equal(1300m, cashSession.TotalExpenses);
        Assert.Equal(500m, cashSession.ClosingBalance);

        var outboxEvents = await verification.OutboxMessages
            .Where(m => m.AggregateId == sale.Id)
            .Select(m => m.EventType)
            .OrderBy(x => x)
            .ToListAsync();
        Assert.Equal(["SaleCancelled", "SaleConfirmed"], outboxEvents);
    }

    private static async Task<SaleFixture> SeedFixtureAsync(SaleIntegrationDbContext db)
    {
        var business = Business.Create("Pollo Sabroso", "DOP", "America/Santo_Domingo", BusinessType.RotisserieChicken);
        var branch = Branch.Create(business.Id, "Sucursal Principal", isMain: true);
        var register = CashRegister.Create(business.Id, branch.Id, "C01", "Caja principal");
        var cash = PaymentMethod.Create(business.Id, "CASH", "Efectivo", PaymentMethodType.Cash);
        var card = PaymentMethod.Create(business.Id, "CARD", "Tarjeta", PaymentMethodType.DebitCard);
        var product = Product.Create(
            business.Id,
            "POL-ENT",
            "Pollo horneado entero",
            ProductType.PreparedItem,
            UnitOfMeasure.Unit,
            650m,
            estimatedCost: 280m,
            tracksInventory: true);

        db.Businesses.Add(business);
        db.Branches.Add(branch);
        db.CashRegisters.Add(register);
        db.PaymentMethods.AddRange(cash, card);
        db.Products.Add(product);
        db.InventoryStocks.Add(InventoryStock.Create(business.Id, branch.Id, product.Id, 10m));
        db.BusinessSettings.Add(BusinessSettings.CreateDefault(business.Id));

        await db.SaveChangesAsync();

        return new SaleFixture(
            business.Id,
            branch.Id,
            register.Id,
            cash.Id,
            card.Id,
            product.Id);
    }

    private sealed record SaleFixture(
        Guid BusinessId,
        Guid BranchId,
        Guid RegisterId,
        Guid CashPaymentMethodId,
        Guid CardPaymentMethodId,
        Guid ProductId);

    private sealed class SaleIntegrationDbContext(DbContextOptions<SaleIntegrationDbContext> options)
        : DbContext(options), IAppDbContext
    {
        public DbSet<Business> Businesses => Set<Business>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductComponent> ProductComponents => Set<ProductComponent>();
        public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
        public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
        public DbSet<ProductionEntry> ProductionEntries => Set<ProductionEntry>();
        public DbSet<ProductionEntryDetail> ProductionEntryDetails => Set<ProductionEntryDetail>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
        public DbSet<CashSession> CashSessions => Set<CashSession>();
        public DbSet<CashMovement> CashMovements => Set<CashMovement>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
        public DbSet<SalePayment> SalePayments => Set<SalePayment>();
        public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
        public DbSet<SaleReturnDetail> SaleReturnDetails => Set<SaleReturnDetail>();
        public DbSet<SaleNumberSequence> SaleNumberSequences => Set<SaleNumberSequence>();
        public DbSet<ReceiptReprintAudit> ReceiptReprintAudits => Set<ReceiptReprintAudit>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<SyncQueueItem> SyncQueueItems => Set<SyncQueueItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Business>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Branch>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasOne(x => x.Business).WithMany(x => x.Branches).HasForeignKey(x => x.BusinessId);
            });

            modelBuilder.Entity<BusinessSettings>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Category>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).IsRequired(false);
            });

            modelBuilder.Entity<ProductComponent>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasOne(x => x.ComponentProduct).WithMany().HasForeignKey(x => x.ComponentProductId);
            });

            modelBuilder.Entity<InventoryStock>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasIndex(x => new { x.BusinessId, x.BranchId, x.ProductId }).IsUnique();
            });

            modelBuilder.Entity<InventoryMovement>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<PaymentMethod>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<CashRegister>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasMany(x => x.Sessions).WithOne(x => x.CashRegister).HasForeignKey(x => x.CashRegisterId);
            });

            modelBuilder.Entity<CashSession>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasMany(x => x.Movements).WithOne(x => x.CashSession).HasForeignKey(x => x.CashSessionId);
            });

            modelBuilder.Entity<CashMovement>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Sale>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasMany(x => x.Details).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId);
                b.HasMany(x => x.Payments).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId);
            });

            modelBuilder.Entity<SaleDetail>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            });

            modelBuilder.Entity<SalePayment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId);
            });

            modelBuilder.Entity<SaleReturn>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasMany(x => x.Details).WithOne(x => x.SaleReturn).HasForeignKey(x => x.SaleReturnId);
            });

            modelBuilder.Entity<SaleReturnDetail>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<SaleNumberSequence>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasIndex(x => new { x.CashRegisterId, x.BusinessDate }).IsUnique();
            });

            modelBuilder.Entity<OutboxMessage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Property(x => x.Payload).IsRequired();
            });

            modelBuilder.Entity<SyncQueueItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<Expense>().HasKey(x => x.Id);
            modelBuilder.Entity<ProductionEntry>().HasKey(x => x.Id);
            modelBuilder.Entity<ProductionEntryDetail>().HasKey(x => x.Id);
            modelBuilder.Entity<ReceiptReprintAudit>().HasKey(x => x.Id);
        }
    }
}

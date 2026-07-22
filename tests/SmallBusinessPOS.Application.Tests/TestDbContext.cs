using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Tests;

/// <summary>
/// DbContext mínimo para pruebas unitarias usando InMemory provider.
/// Implementa IAppDbContext directamente sin Identity.
/// </summary>
public class TestDbContext : DbContext, IAppDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

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
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Business>().HasKey(b => b.Id);
        modelBuilder.Entity<Business>().Property(b => b.Id).ValueGeneratedNever();

        modelBuilder.Entity<Branch>().HasKey(b => b.Id);
        modelBuilder.Entity<Branch>().Property(b => b.Id).ValueGeneratedNever();

        modelBuilder.Entity<BusinessSettings>().HasKey(bs => bs.Id);
        modelBuilder.Entity<BusinessSettings>().Property(bs => bs.Id).ValueGeneratedNever();

        modelBuilder.Entity<Category>().HasKey(c => c.Id);
        modelBuilder.Entity<Category>().Property(c => c.Id).ValueGeneratedNever();

        modelBuilder.Entity<Customer>().HasKey(c => c.Id);
        modelBuilder.Entity<Customer>().Property(c => c.Id).ValueGeneratedNever();

        modelBuilder.Entity<Product>().HasKey(p => p.Id);
        modelBuilder.Entity<Product>().Property(p => p.Id).ValueGeneratedNever();
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .IsRequired(false);

        modelBuilder.Entity<ProductComponent>().HasKey(pc => pc.Id);
        modelBuilder.Entity<ProductComponent>().Property(pc => pc.Id).ValueGeneratedNever();
        modelBuilder.Entity<ProductComponent>()
            .HasOne(pc => pc.ParentProduct)
            .WithMany(p => p.Components)
            .HasForeignKey(pc => pc.ParentProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductComponent>()
            .HasOne(pc => pc.ComponentProduct)
            .WithMany()
            .HasForeignKey(pc => pc.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryStock>().HasKey(s => s.Id);
        modelBuilder.Entity<InventoryStock>().Property(s => s.Id).ValueGeneratedNever();

        modelBuilder.Entity<InventoryMovement>().HasKey(m => m.Id);
        modelBuilder.Entity<InventoryMovement>().Property(m => m.Id).ValueGeneratedNever();

        modelBuilder.Entity<ProductionEntry>().HasKey(pe => pe.Id);
        modelBuilder.Entity<ProductionEntry>().Property(pe => pe.Id).ValueGeneratedNever();
        modelBuilder.Entity<ProductionEntry>()
            .HasMany(pe => pe.Details)
            .WithOne(d => d.ProductionEntry)
            .HasForeignKey(d => d.ProductionEntryId);

        modelBuilder.Entity<ProductionEntryDetail>().HasKey(pd => pd.Id);
        modelBuilder.Entity<ProductionEntryDetail>().Property(pd => pd.Id).ValueGeneratedNever();

        modelBuilder.Entity<PaymentMethod>().HasKey(pm => pm.Id);
        modelBuilder.Entity<PaymentMethod>().Property(pm => pm.Id).ValueGeneratedNever();

        modelBuilder.Entity<CashRegister>().HasKey(cr => cr.Id);
        modelBuilder.Entity<CashRegister>().Property(cr => cr.Id).ValueGeneratedNever();

        modelBuilder.Entity<CashSession>().HasKey(cs => cs.Id);
        modelBuilder.Entity<CashSession>().Property(cs => cs.Id).ValueGeneratedNever();

        modelBuilder.Entity<CashMovement>().HasKey(cm => cm.Id);
        modelBuilder.Entity<CashMovement>().Property(cm => cm.Id).ValueGeneratedNever();

        modelBuilder.Entity<Expense>().HasKey(e => e.Id);
        modelBuilder.Entity<Expense>().Property(e => e.Id).ValueGeneratedNever();

        modelBuilder.Entity<Sale>().HasKey(s => s.Id);
        modelBuilder.Entity<Sale>().Property(s => s.Id).ValueGeneratedNever();

        modelBuilder.Entity<SaleDetail>().HasKey(sd => sd.Id);
        modelBuilder.Entity<SaleDetail>().Property(sd => sd.Id).ValueGeneratedNever();

        modelBuilder.Entity<SalePayment>().HasKey(sp => sp.Id);
        modelBuilder.Entity<SalePayment>().Property(sp => sp.Id).ValueGeneratedNever();

        modelBuilder.Entity<SaleReturn>().HasKey(sr => sr.Id);
        modelBuilder.Entity<SaleReturn>().Property(sr => sr.Id).ValueGeneratedNever();
        modelBuilder.Entity<SaleReturn>()
            .HasMany(sr => sr.Details)
            .WithOne(detail => detail.SaleReturn)
            .HasForeignKey(detail => detail.SaleReturnId);

        modelBuilder.Entity<SaleReturnDetail>().HasKey(detail => detail.Id);
        modelBuilder.Entity<SaleReturnDetail>().Property(detail => detail.Id).ValueGeneratedNever();

        modelBuilder.Entity<SaleNumberSequence>().HasKey(ss => ss.Id);
        modelBuilder.Entity<SaleNumberSequence>().Property(ss => ss.Id).ValueGeneratedNever();

        modelBuilder.Entity<ReceiptReprintAudit>().HasKey(r => r.Id);
        modelBuilder.Entity<ReceiptReprintAudit>().Property(r => r.Id).ValueGeneratedNever();

        modelBuilder.Entity<OutboxMessage>().HasKey(om => om.Id);
        modelBuilder.Entity<OutboxMessage>().Property(om => om.Id).ValueGeneratedNever();

        modelBuilder.Entity<SyncQueueItem>().HasKey(s => s.Id);
        modelBuilder.Entity<SyncQueueItem>().Property(s => s.Id).ValueGeneratedNever();
    }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Application.Interfaces;
using SmallBusinessPOS.Domain.Entities;
using SmallBusinessPOS.Infrastructure.Data.Configurations;
using SmallBusinessPOS.Infrastructure.Data.Identity;

namespace SmallBusinessPOS.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos principal.
/// Actúa como Unit of Work.
/// Extiende IdentityDbContext para incluir tablas de ASP.NET Core Identity.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTypeOption> ProductTypeOptions => Set<ProductTypeOption>();
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Cada configuración en su propio archivo
        builder.ApplyConfiguration(new BusinessConfiguration());
        builder.ApplyConfiguration(new BranchConfiguration());
        builder.ApplyConfiguration(new BusinessSettingsConfiguration());
        builder.ApplyConfiguration(new CategoryConfiguration());
        builder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        builder.ApplyConfiguration(new CustomerConfiguration());
        builder.ApplyConfiguration(new ProductConfiguration());
        builder.ApplyConfiguration(new ProductTypeOptionConfiguration());
        builder.ApplyConfiguration(new ProductComponentConfiguration());
        builder.ApplyConfiguration(new InventoryStockConfiguration());
        builder.ApplyConfiguration(new InventoryMovementConfiguration());
        builder.ApplyConfiguration(new ProductionEntryConfiguration());
        builder.ApplyConfiguration(new ProductionEntryDetailConfiguration());
        builder.ApplyConfiguration(new PaymentMethodConfiguration());
        builder.ApplyConfiguration(new CashRegisterConfiguration());
        builder.ApplyConfiguration(new CashSessionConfiguration());
        builder.ApplyConfiguration(new CashMovementConfiguration());
        builder.ApplyConfiguration(new ExpenseConfiguration());
        builder.ApplyConfiguration(new SaleConfiguration());
        builder.ApplyConfiguration(new SaleDetailConfiguration());
        builder.ApplyConfiguration(new SalePaymentConfiguration());
        builder.ApplyConfiguration(new SaleReturnConfiguration());
        builder.ApplyConfiguration(new SaleReturnDetailConfiguration());
        builder.ApplyConfiguration(new SaleNumberSequenceConfiguration());
        builder.ApplyConfiguration(new ReceiptReprintAuditConfiguration());
        builder.ApplyConfiguration(new OutboxMessageConfiguration());
        builder.ApplyConfiguration(new SyncQueueItemConfiguration());

        // Prefijo de tablas de Identity
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");
    }
}

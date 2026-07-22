using Microsoft.EntityFrameworkCore;
using SmallBusinessPOS.Domain.Entities;

namespace SmallBusinessPOS.Application.Interfaces;

/// <summary>
/// Abstracción del contexto de datos.
/// Implementada por AppDbContext en Infrastructure.
/// Application no depende de la implementación concreta de EF Core,
/// solo de los DbSet expuestos aquí.
/// </summary>
public interface IAppDbContext
{
    DbSet<Business> Businesses { get; }
    DbSet<Branch> Branches { get; }
    DbSet<BusinessSettings> BusinessSettings { get; }
    DbSet<Category> Categories { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductTypeOption> ProductTypeOptions { get; }
    DbSet<ProductComponent> ProductComponents { get; }
    DbSet<InventoryStock> InventoryStocks { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<ProductionEntry> ProductionEntries { get; }
    DbSet<ProductionEntryDetail> ProductionEntryDetails { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<CashRegister> CashRegisters { get; }
    DbSet<CashSession> CashSessions { get; }
    DbSet<CashMovement> CashMovements { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleDetail> SaleDetails { get; }
    DbSet<SalePayment> SalePayments { get; }
    DbSet<SaleReturn> SaleReturns { get; }
    DbSet<SaleReturnDetail> SaleReturnDetails { get; }
    DbSet<SaleNumberSequence> SaleNumberSequences { get; }
    DbSet<ReceiptReprintAudit> ReceiptReprintAudits { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<SyncQueueItem> SyncQueueItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

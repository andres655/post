using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Gasto operativo del negocio. Si se paga desde caja, referencia la sesion que lo cubrio.
/// </summary>
public class Expense : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid? CashSessionId { get; private set; }
    public Guid? ExpenseCategoryId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Concept { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Notes { get; private set; }

    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public CashSession? CashSession { get; private set; }
    public ExpenseCategory? ExpenseCategory { get; private set; }

    private Expense() { }

    public static Expense Create(
        Guid businessId,
        Guid branchId,
        string category,
        string concept,
        decimal amount,
        Guid? cashSessionId = null,
        Guid? expenseCategoryId = null,
        string? notes = null,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(concept);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto debe ser mayor a cero.");

        var expense = new Expense
        {
            BusinessId = businessId,
            BranchId = branchId,
            CashSessionId = cashSessionId,
            ExpenseCategoryId = expenseCategoryId,
            Category = category.Trim(),
            Concept = concept.Trim(),
            Amount = amount,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };

        if (createdBy is not null)
            expense.SetCreatedBy(createdBy);

        return expense;
    }
}

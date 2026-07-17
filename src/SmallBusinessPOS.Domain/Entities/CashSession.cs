using SmallBusinessPOS.Domain.Common;
using SmallBusinessPOS.Domain.Enums;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Sesión de caja — abierta por un usuario, registra movimientos, se cierra.
/// Una sola sesión activa por caja registradora.
/// </summary>
public class CashSession : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid CashRegisterId { get; private set; }
    public CashSessionStatus Status { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal TotalIncome { get; private set; }
    public decimal TotalExpenses { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public decimal DeclaredClosingBalance { get; private set; }
    public decimal Difference { get; private set; }
    public string? Notes { get; private set; }

    // Navegación
    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public CashRegister CashRegister { get; private set; } = null!;
    private readonly List<CashMovement> _movements = new();
    public IReadOnlyCollection<CashMovement> Movements => _movements.AsReadOnly();

    private CashSession() { }

    public static CashSession Create(
        Guid businessId,
        Guid branchId,
        Guid cashRegisterId,
        decimal openingBalance,
        string? openedBy = null)
    {
        var session = new CashSession
        {
            BusinessId = businessId,
            BranchId = branchId,
            CashRegisterId = cashRegisterId,
            Status = CashSessionStatus.Open,
            OpenedAtUtc = DateTime.UtcNow,
            OpeningBalance = openingBalance,
            TotalIncome = 0m,
            TotalExpenses = 0m,
            ClosingBalance = openingBalance,
            DeclaredClosingBalance = 0m,
            Difference = 0m
        };

        if (openedBy is not null)
            session.SetCreatedBy(openedBy);

        return session;
    }

    /// <summary>
    /// Agrega un ingreso a la sesión.
    /// </summary>
    public void AddIncome(decimal amount)
    {
        if (Status != CashSessionStatus.Open)
            throw new InvalidOperationException("No se puede agregar ingresos a una sesión cerrada.");

        TotalIncome += amount;
        ClosingBalance += amount;
    }

    /// <summary>
    /// Agrega un gasto a la sesión.
    /// </summary>
    public void AddExpense(decimal amount)
    {
        if (Status != CashSessionStatus.Open)
            throw new InvalidOperationException("No se puede agregar gastos a una sesión cerrada.");

        TotalExpenses += amount;
        ClosingBalance -= amount;
    }

    /// <summary>
    /// Cierra la sesión con el balance declarado.
    /// </summary>
    public void Close(decimal declaredBalance, string? notes = null, string? closedBy = null)
    {
        if (Status != CashSessionStatus.Open)
            throw new InvalidOperationException("La sesión ya está cerrada.");

        Status = CashSessionStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
        DeclaredClosingBalance = declaredBalance;
        Difference = declaredBalance - ClosingBalance;
        Notes = notes;

        if (closedBy is not null)
            SetUpdated(closedBy);
    }
}

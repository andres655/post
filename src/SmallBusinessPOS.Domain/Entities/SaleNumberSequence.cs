using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>
/// Secuencia local de numeración de ventas por caja y fecha.
/// Permite generar números offline seguros por cada caja.
/// </summary>
public class SaleNumberSequence : Entity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid CashRegisterId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public int LastSequence { get; private set; }

    /// <summary>Token de concurrencia para incrementos seguros.</summary>
    public byte[]? RowVersion { get; private set; }

    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    public CashRegister CashRegister { get; private set; } = null!;

    private SaleNumberSequence() { }

    public static SaleNumberSequence Create(
        Guid businessId,
        Guid branchId,
        Guid cashRegisterId,
        DateOnly businessDate)
    {
        return new SaleNumberSequence
        {
            BusinessId = businessId,
            BranchId = branchId,
            CashRegisterId = cashRegisterId,
            BusinessDate = businessDate,
            LastSequence = 0
        };
    }

    public int IncrementAndGet()
    {
        LastSequence++;
        return LastSequence;
    }
}

using SmallBusinessPOS.Domain.Common;

namespace SmallBusinessPOS.Domain.Entities;

/// <summary>Caja registradora en una sucursal.</summary>
public class CashRegister : AuditableEntity, ISynchronizableEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navegación
    public Business Business { get; private set; } = null!;
    public Branch Branch { get; private set; } = null!;
    private readonly List<CashSession> _sessions = new();
    public IReadOnlyCollection<CashSession> Sessions => _sessions.AsReadOnly();

    private CashRegister() { }

    public static CashRegister Create(
        Guid businessId,
        Guid branchId,
        string code,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new CashRegister
        {
            BusinessId = businessId,
            BranchId = branchId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsActive = true
        };
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
